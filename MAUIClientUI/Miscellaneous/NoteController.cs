using DatabaseLibrary.Cursor;
using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using Microsoft.Extensions.Logging;

namespace MAUIClientUI.Miscellaneous
{
    /// <summary>
    /// Central place for all note logic. Owns the CRDT cursor, the repositories and the note service
    /// so the rest of the app (views, platform input handlers) never touch the CRDT model or the
    /// transport directly. Input handlers only decode keystrokes and call <see cref="InsertCharacter"/> /
    /// <see cref="DeleteCharacter"/>; remote updates are applied through <see cref="ApplyRemoteChangeAsync"/>.
    /// </summary>
    public class NoteController
    {
        private readonly NoteCursor _noteCursor;
        private readonly NoteClient _currentNote;
        private readonly CRDTCharacterRepository _crdtCharacterRepository;
        private readonly NoteRepository _noteRepository;
        private readonly INoteServices _noteServices;

        public NoteController(NoteClient currentNote, NoteRepository noteRepository,
            INoteServices noteServices, ILogger<NoteCursor> cursorLogger = null)
        {
            _currentNote = currentNote ?? throw new ArgumentNullException(nameof(currentNote));
            _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
            _noteServices = noteServices ?? throw new ArgumentNullException(nameof(noteServices));
            _crdtCharacterRepository = IPlatformApplication.Current.Services.GetService<CRDTCharacterRepository>()
                ?? throw new InvalidOperationException($"{nameof(CRDTCharacterRepository)} is not registered.");
            _noteCursor = new NoteCursor(_currentNote.CRDTCharacter, Guid.NewGuid(), cursorLogger);
        }

        /// <summary>
        /// Returns the current text of the note as reconstructed from the CRDT model.
        /// </summary>
        public string GetText() => _noteCursor.GetString();

        /// <summary>
        /// Inserts a character at the given cursor position and propagates the change.
        /// </summary>
        public void InsertCharacter(int cursorPosition, char typedChar)
        {
            var newCharacter = _noteCursor.InsertCharacter(cursorPosition, typedChar);
            newCharacter.IdNote = _currentNote.IdNote;
            _crdtCharacterRepository.SaveNewCrdtCharacter(newCharacter);
            _ = SendChangeToServerAsync(newCharacter);
        }

        /// <summary>
        /// Deletes the character to the left of the given cursor position and propagates the change.
        /// </summary>
        public void DeleteCharacter(int cursorPosition)
        {
            var leftCharacter = _noteCursor.deleteCharacter(cursorPosition + 1);
            if (leftCharacter != null)
            {
                _crdtCharacterRepository.UpdateCharacter(leftCharacter);
                _ = SendChangeToServerAsync(leftCharacter);
            }
        }

        /// <summary>
        /// Persists a character received from another user/device and merges it into the CRDT model.
        /// Returns true when the change belongs to this note and was applied.
        /// </summary>
        public async Task<bool> ApplyRemoteChangeAsync(CRDTCharacter character)
        {
            if (character.IdNote != _currentNote.IdNote)
                return false;

            var clientCharacter = new CRDTCharacterClient(character);
            await _noteRepository.SaveCRDTChanges(new List<CRDTCharacterClient> { clientCharacter });
            _noteCursor.MergeCharacter(clientCharacter);
            return true;
        }

        private Task SendChangeToServerAsync(CRDTCharacter change)
            => _noteServices.SendCRDTChangestoServer(new List<CRDTCharacter> { change });
    }
}


