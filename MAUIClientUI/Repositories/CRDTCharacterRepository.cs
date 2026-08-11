using DatabaseLibrary.Entities.Client;
using System;
using System.Collections.Generic;
using System.Text;

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
            return _dbContext.CRDTCharacters.Where(n => n.IdNote == IdNote).ToList(); // shuold this be async or not i dont know
        }

        public void SaveNewCrdtCharacter(CRDTCharacterClient newCharacter)
        {
            _dbContext.CRDTCharacters.Add(newCharacter);
            a_dbContext.SaveChanges();
        }

        public void UpdateCharacter(CRDTCharacterClient updateCharacter)
        {
            _dbContext.CRDTCharacters.Update(updateCharacter);
            _dbContext.SaveChanges();
        }
    }
}
