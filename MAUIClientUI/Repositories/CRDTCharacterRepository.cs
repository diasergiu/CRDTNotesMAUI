using DatabaseLibrary.Entities.Client;
using Microsoft.EntityFrameworkCore;

namespace MAUIClientUI.Repositories
{
    public class CRDTCharacterRepository
    {
        private DbContextClient _dbContext;
        public CRDTCharacterRepository(DbContextClient dbContextUser)
        {
            _dbContext = dbContextUser;
        }


        public List<CRDTCharacterClient> GetCRDTCharacterFromNote(Guid IdNote)
        {
            // Use AsNoTracking to prevent EF Core from tracking these entities
            // This avoids conflicts when updating later
            return _dbContext.CRDTCharacters
                .AsNoTracking()
                .Where(n => n.IdNote == IdNote)
                .ToList();
        }

        public virtual void SaveNewCrdtCharacter(CRDTCharacterClient newCharacter)
        {
            _dbContext.ChangeTracker.Clear();

            // Check if character exists using AsNoTracking to avoid tracking conflicts
            var existing = _dbContext.CRDTCharacters
                .AsNoTracking()
                .FirstOrDefault(n => n.IdCharacter == newCharacter.IdCharacter && n.IdNote == newCharacter.IdNote);

            if (existing != null)
            {
                UpdateCharacter(newCharacter);
                return;
            }

            _dbContext.CRDTCharacters.Add(newCharacter);
            _dbContext.SaveChanges();
            _dbContext.ChangeTracker.Clear();
        }

        public virtual void UpdateCharacter(CRDTCharacterClient updateCharacter)
        {
            _dbContext.ChangeTracker.Clear();
            _dbContext.CRDTCharacters.Update(updateCharacter);
            _dbContext.SaveChanges();
            _dbContext.ChangeTracker.Clear();
        }
    }
}
