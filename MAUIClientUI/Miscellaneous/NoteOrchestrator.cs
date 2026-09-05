using CRDTLibrary.Cursor;
using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;
using Microsoft.Extensions.Logging;

namespace MAUIClientUI.Miscellaneous
{
    /// <summary>
    /// Central place for all note logic. Owns the CRDT cursor, the repositories and the note service
    /// so the rest of the app (views, platform input handlers) never touch the CRDT model or the
    /// transport directly. Input handlers only decode keystrokes and call <see cref="InsertCharacter"/> /
    /// <see cref="DeleteCharacter"/>; remote updates are applied through <see cref="ApplyRemoteChangeAsync"/>.
    /// </summary>
    public class NoteOrchestrator
    {
        private readonly Document _Document;
        private readonly Guid _IdNote;
        private readonly CRDTCharacterRepository _crdtCharacterRepository;
        private readonly NoteRepository _noteRepository;
        private readonly INoteServices _noteServices;
        private readonly ILogger<NoteOrchestrator>? _logger;

        public NoteOrchestrator(NoteClient currentNote, NoteRepository noteRepository,
            INoteServices noteServices,  CRDTCharacterRepository characterRepository, ILogger<Document> cursorLogger = null, ILogger<NoteOrchestrator> logger = null)
        {
            _IdNote= currentNote.IdNote;
            _noteRepository = noteRepository;
            _noteServices = noteServices;
            _crdtCharacterRepository = characterRepository;
            _logger = logger;

            // Convert CRDTCharacterClient list to CRDTCharacterPayload list for Document
            var payloads = currentNote.CRDTCharacter?.Select(c => UnwrapClientAsPayload(c)).ToList()
                ?? new List<CRDTCharacterPayload>();
            _Document = new Document(payloads, Guid.NewGuid(), cursorLogger);// Guid.NewGuid should probably be client Id ( probably refactor if login becomes the main page
        }

        /// <summary>
        /// Returns the current text of the note as reconstructed from the CRDT model.
        /// </summary>
        public string GetText() => _Document.GetString();

        /// <summary>
        /// Inserts a character at the given cursor position and propagates the change.
        /// </summary>
        public async Task InsertCharacter(int cursorPosition, char typedChar)
        {
            // Get payload from Document (pure CRDT logic)
            var newCharacter = _Document.InsertCharacter(cursorPosition, typedChar);

            // Wrap in client object with application-level metadata (idNote, isDirtyFlag, etc.)
            var clientCharacter = WrapPayloadAsClient(newCharacter);

            // Persist locally
            _crdtCharacterRepository.SaveNewCrdtCharacter(clientCharacter);

            // Mark the note as dirty
            MarkNoteAsDirty();

            // Send to server (payload used for encryption, conversion handled inside)
            await SendChangeToServerAsync(newCharacter);
        }
        /// <summary>
        /// Inserts multiple characters (e.g., from paste operation) at the given cursor position.
        /// </summary>
        public async Task InsertString(int cursorPosition, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var newPayloads = new List<CRDTCharacterPayload>();
            foreach (char c in text)
            {
                // Get payload from Document
                var payload = _Document.InsertCharacter(cursorPosition++, c);

                // Wrap in client object for persistence
                var clientCharacter = WrapPayloadAsClient(payload);
                _crdtCharacterRepository.SaveNewCrdtCharacter(clientCharacter);

                newPayloads.Add(payload);
            }

            // Mark the note as dirty
            MarkNoteAsDirty();

            // Send batch of payloads to server
            await SendChangesToServerAsync(newPayloads);
        }

        /// <summary>
        /// Async version of DeleteCharacter. Deletes the character to the left of the given cursor position and propagates the change.
        /// </summary>
        public async Task DeleteCharacter(int cursorPosition)
       {
            // Get payload from Document
            var payload = _Document.DeleteCharacter(cursorPosition + 1);
          if (payload != null)
            {
                // Wrap in client object for persistence
                var clientCharacter = WrapPayloadAsClient(payload);
                _crdtCharacterRepository.UpdateCharacter(clientCharacter);

                MarkNoteAsDirty();
                // Send to server
                await SendChangeToServerAsync(payload);
            }
        }

        /// <summary>
        /// Deletes multiple characters in a range (e.g., when user selects text and deletes).
        /// </summary>
        public async Task DeleteCharacterRange(int startPosition, int endPosition)
        {
            if (startPosition >= endPosition)
                return;

            var deletedPayloads = new List<CRDTCharacterPayload>();
            // Delete from end to start to avoid position shifting issues
            for (int i = endPosition; i > startPosition; i--)
            {
                // Get payload from Document
                var payload = _Document.DeleteCharacter(i);
                if (payload != null)
                {
                    // Wrap in client object for persistence
                    var clientCharacter = WrapPayloadAsClient(payload);
                    _crdtCharacterRepository.UpdateCharacter(clientCharacter);

                    // Mark the note as dirty
                    MarkNoteAsDirty();

                    deletedPayloads.Add(payload);
                }
            }

            // Send batch of payloads to server
            await SendChangesToServerAsync(deletedPayloads);
        }

        /// <summary>
        /// Persists multiple characters received from another user/device and merges them into the CRDT model.
        /// Returns true when all changes belong to this note and were applied.
        /// </summary>
        public async Task<bool> ApplyRemoteChangesAsync(CRDTChangePayload payload)
        {
            if (payload != null && !string.IsNullOrEmpty(payload.Payload))
            {
                // Decode the encrypted payload to get the list of CRDT character changes
                var decodedCharacters = CharacterSerializer.Decode(payload.Payload);

                var characters = decodedCharacters.Select(c => new CRDTCharacterClient
                {
                    IdCharacter = c.IdCharacter,
                    IdNote = payload.IdNote, // Ensure the correct note ID is set
                    Character = c.Character,
                    Tombstone = c.Tombstone,
                    IsDirtyFlag = false, // Received changes are not dirty
                    ClockDateTime = DateTime.UtcNow // Set to current time for local tracking
                }).ToList();

                // Persist all changes
                await _noteRepository.SaveCRDTChanges(characters).ConfigureAwait(true);

                // Convert to payloads and merge into CRDT model
                foreach (var character in decodedCharacters)
                {
                    _Document.MergeCharacter(character);
                }
            }
            return true;

        }

        private async Task SendChangeToServerAsync(CRDTCharacterPayload payload)
        {
            try
            {
                // Wrap payload in a list for encoding
                var encodedPayload = CharacterSerializer.Encode(new List<CRDTCharacterClient> { UnwrapPayloadAsClient(payload) });
                var changePayload = new CRDTChangePayload(_IdNote, encodedPayload);
                var result = await _noteServices.SendCRDTChangestoServer(changePayload);

                if (!result.IsSuccess)
                {
                    _logger?.LogWarning("Failed to send character change to server: {ErrorMessage}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while sending character change to server");
            }
        }

        private async Task SendChangesToServerAsync(List<CRDTCharacterPayload> payloads)
        {
            try
            {
                // Convert payloads to client objects for encoding
                var clientCharacters = payloads.Select(p => UnwrapPayloadAsClient(p)).ToList();
                var encodedPayload = CharacterSerializer.Encode(clientCharacters);
                var changePayload = new CRDTChangePayload(_IdNote, encodedPayload);
                var result = await _noteServices.SendCRDTChangestoServer(changePayload);

                if (!result.IsSuccess)
                {
                    _logger?.LogWarning("Failed to send {Count} character changes to server: {ErrorMessage}", payloads.Count, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while sending {Count} character changes to server", payloads.Count);
            }
        }

        /// <summary>
        /// Wraps a CRDTCharacterPayload in a CRDTCharacterClient with application metadata.
        /// </summary>
        private CRDTCharacterClient WrapPayloadAsClient(CRDTCharacterPayload payload)
        {
            return new CRDTCharacterClient
            {
                IdCharacter = payload.IdCharacter,
                Character = payload.Character,
                Tombstone = payload.Tombstone,
                IdNote = _IdNote,
                IsDirtyFlag = true,
                ClockDateTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Unwraps a CRDTCharacterClient back to a pure CRDTCharacterPayload for CRDT operations.
        /// </summary>
        private CRDTCharacterPayload UnwrapClientAsPayload(CRDTCharacterClient client)
        {
            return new CRDTCharacterPayload
            {
                IdCharacter = client.IdCharacter,
                Character = client.Character,
                Tombstone = client.Tombstone
            };
        }

        /// <summary>
        /// Converts a CRDTCharacterPayload to CRDTCharacterClient for encoding/transmission.
        /// </summary>
        private CRDTCharacterClient UnwrapPayloadAsClient(CRDTCharacterPayload payload)
        {
            return new CRDTCharacterClient
            {
                IdCharacter = payload.IdCharacter,
                Character = payload.Character,
                Tombstone = payload.Tombstone,
                IdNote = _IdNote,
                IsDirtyFlag = false,  // Doesn't matter for encoding
                ClockDateTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Marks the note as modified offline and persists the change to the repository.
        /// Called whenever content changes to ensure sync tracking when offline.
        /// </summary>
        private void MarkNoteAsDirty()
        {
            _noteRepository.MarkNoteAsDirty(_IdNote);
        }
    }
}


