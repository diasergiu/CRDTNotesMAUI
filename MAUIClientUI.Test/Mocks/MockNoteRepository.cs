using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Repositories;

namespace MAUIClientUI.Test.Mocks
{

    public class MockNoteRepository : NoteRepository
    {
        private readonly List<CRDTCharacterClient> _characters = new();
        private List<NoteClient> _mockNotes = new();

        public MockNoteRepository() : base(new DbContextClient())
        {
        }


        public IReadOnlyList<CRDTCharacterClient> GetAllCharacters() => _characters.AsReadOnly();


        public void SetMockNotes(List<NoteClient> notes)
        {
            _mockNotes = notes ?? new List<NoteClient>();
        }


        public List<NoteClient> GetMockNotes()
        {
            return _mockNotes;
        }


        public override async Task SaveCRDTChanges(List<CRDTCharacterClient> changes)
        {
            if (changes != null)
            {
                foreach (var change in changes)
                {
                    var existing = _characters.FirstOrDefault(c => c.IdCharacter == change.IdCharacter);
                    if (existing != null)
                    {
                        _characters.Remove(existing);
                    }
                    _characters.Add(change);
                }
            }
            await Task.CompletedTask;
        }
        public void Clear()
        {
            _characters.Clear();
            _mockNotes.Clear();
        }
    }
}
