using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Repositories;

namespace EndToEndTest.Mocks
{

    public class MockNoteRepository : NoteRepository
    {
        private readonly List<CRDTCharacterClient> _characters = new();
        private List<NoteClient> _mockNotes = new();
        private Dictionary<Guid, NoteClient> _mockNotesDict = new();

        public MockNoteRepository() : base(new DbContextClient())
        {
        }


        public IReadOnlyList<CRDTCharacterClient> GetAllCharacters() => _characters.AsReadOnly();


        public void SetMockNotes(List<NoteClient> notes)
        {
            _mockNotes = notes ?? new List<NoteClient>();
            _mockNotesDict = _mockNotes.ToDictionary(n => n.IdNote);
        }


        public List<NoteClient> GetMockNotes()
        {
            return _mockNotes;
        }

        /// <summary>
        /// Override UpdateNote to avoid DbContext issues in tests.
        /// Instead of using DbContext, just update the in-memory mock.
        /// </summary>
        public override void UpdateNote(NoteClient note)
        {
            if (note == null) return;

            // Update the in-memory mock dictionary
            if (_mockNotesDict.ContainsKey(note.IdNote))
            {
                _mockNotesDict[note.IdNote] = note;
            }
            else
            {
                _mockNotesDict.Add(note.IdNote, note);
            }

            // Also update the list
            var existing = _mockNotes.FirstOrDefault(n => n.IdNote == note.IdNote);
            if (existing != null)
            {
                _mockNotes.Remove(existing);
            }
            _mockNotes.Add(note);
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
            _mockNotesDict.Clear();
        }
    }
}

