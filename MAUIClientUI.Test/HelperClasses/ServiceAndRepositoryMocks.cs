using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;

namespace MAUIClientUI.Test.HelperClasses
{
    public class InMemoryNoteRepository : NoteRepository
    {
        private readonly List<CRDTCharacterClient> _characters = new();
        private readonly object _sync = new();

        public InMemoryNoteRepository() : base(new DbContextClient())
        {
        }

        public IReadOnlyList<CRDTCharacterClient> Characters
        {
            get { lock (_sync) { return _characters.ToList(); } }
        }

        public override void MarkNoteAsDirty(Guid IdNote)
        {
            // No-op: dirty tracking is a persistence concern, irrelevant to these tests.
        }

        public override async Task SaveCRDTChanges(List<CRDTCharacterClient> changes)
        {
            if (changes != null)
            {
                lock (_sync)
                {
                    foreach (var change in changes)
                    {
                        _characters.RemoveAll(c => c.IdCharacter == change.IdCharacter);
                        _characters.Add(change);
                    }
                }
            }

            // Yield so callers actually suspend here, the way a real async save would.
            await Task.Yield();
        }
    }
    public class InMemoryCRDTCharacterRepository : CRDTCharacterRepository
    {
        private readonly List<CRDTCharacterClient> _characters = new();
        private readonly object _sync = new();

        public InMemoryCRDTCharacterRepository() : base(new DbContextClient())
        {
        }

        public IReadOnlyList<CRDTCharacterClient> Characters
        {
            get { lock (_sync) { return _characters.ToList(); } }
        }

        public override void SaveNewCrdtCharacter(CRDTCharacterClient character)
        {
            if (character == null) return;
            lock (_sync) { _characters.Add(character); }
        }

        public override void UpdateCharacter(CRDTCharacterClient character)
        {
            if (character == null) return;
            lock (_sync)
            {
                _characters.RemoveAll(c => c.IdCharacter == character.IdCharacter);
                _characters.Add(character);
            }
        }
    }

    public class RecordingNoteServices : INoteServices
    {
        private readonly List<CRDTChangePayload> _sentChanges = new();
        private readonly object _sync = new();

        public Func<CRDTChangePayload, Task>? OnCRDTChangeSent { get; set; }

        public IReadOnlyList<CRDTChangePayload> SentChanges
        {
            get { lock (_sync) { return _sentChanges.ToList(); } }
        }

        public async Task<ApiResult> SendCRDTChangestoServer(CRDTChangePayload payload)
        {
            lock (_sync) { _sentChanges.Add(payload); }

            // Yield so the send is a genuine async suspension point.
            await Task.Yield();

            var callback = OnCRDTChangeSent;
            if (callback != null)
                await callback(payload);

            return new ApiResult { IsSuccess = true };
        }

        public void Clear()
        {
            lock (_sync) { _sentChanges.Clear(); }
        }

        public Task<ApiResultData<List<NoteClient>>> GetAllNotesFromUser(Guid IdUser) => throw new NotImplementedException();
        public Task<ApiResultData<List<NoteServer>>> GetServerChanges() => throw new NotImplementedException();
        public Task<ApiResult> CreateNewNote(NoteClient currentNote) => throw new NotImplementedException();
        public Task<ApiResultData<NoteConflictResult>> UpdateNote(NoteClient updatedNote) => throw new NotImplementedException();
        public Task<ApiResult> DeleteNote(Guid noteId) => throw new NotImplementedException();
        public Task<ApiResultData<List<NoteServer>>> SendChangesToServer(List<DTOSendChanges> noteClient) => throw new NotImplementedException();
        public Task<ApiResultData<List<DTOSendChanges>>> GetServerChangesByNote(Guid noteId) => throw new NotImplementedException();
        public Task<ApiResultData<NoteClient>> GetNote(Guid noteId) => throw new NotImplementedException();
        public Task<ApiResult> GiveNoteAccessToUser(Guid noteId, string userId) => throw new NotImplementedException();
    }
}
