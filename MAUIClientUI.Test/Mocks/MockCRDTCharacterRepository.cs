using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Repositories;

namespace MAUIClientUI.Test.Mocks
{
    /// <summary>
    /// Mock implementation of CRDTCharacterRepository for testing.
    /// Stores CRDT characters in memory without database persistence.
    /// </summary>
    public class MockCRDTCharacterRepository : CRDTCharacterRepository
    {
        private readonly List<CRDTCharacterClient> _characters = new();

        public MockCRDTCharacterRepository() : base(new TestDbContextClient())
        {
        }

        /// <summary>
        /// Gets all stored characters.
        /// </summary>
        public IReadOnlyList<CRDTCharacterClient> GetAllCharacters() => _characters.AsReadOnly();

        /// <summary>
        /// Saves a new CRDT character to the in-memory store.
        /// </summary>
        public override void SaveNewCrdtCharacter(CRDTCharacterClient character)
        {
            if (character != null)
            {
                _characters.Add(character);
            }
        }

        /// <summary>
        /// Updates an existing character in the in-memory store.
        /// </summary>
        public override void UpdateCharacter(CRDTCharacterClient character)
        {
            if (character != null)
            {
                var existing = _characters.FirstOrDefault(c => c.IdCharacter == character.IdCharacter);
                if (existing != null)
                {
                    _characters.Remove(existing);
                    _characters.Add(character);
                }
                else
                {
                    _characters.Add(character);
                }
            }
        }

        /// <summary>
        /// Clears all stored characters for test isolation.
        /// </summary>
        public void Clear()
        {
            _characters.Clear();
        }
    }

    /// <summary>
    /// Minimal test implementation of DbContextClient for in-memory testing.
    /// </summary>
    internal class TestDbContextClient : DbContextClient
    {
        public TestDbContextClient() : base()
        {
        }
    }
}
