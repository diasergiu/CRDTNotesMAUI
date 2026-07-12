using DatabaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;

namespace Server.Repositories
{
    public class UserRepository
    {
        private DbContextServer DbContextServer;

        public UserRepository(DbContextServer context)
        {
            DbContextServer = context;
        }


        public async Task<List<Note>> GetServerNotesByUserId(int idUser)
        {
            try
            {
                var serverNotes = await DbContextServer.Notes
                    .Where(sn => sn.NoteUser.Any(snu => snu.IdUser == idUser))
                    .ToListAsync();
                return serverNotes;

            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it)
                throw new Exception("An error occurred while retrieving server notes.", ex);
            }
        }

        public async Task<User> GetUserByNameAndPassword(string name, string password)
        {
            try
            {
                var user = await DbContextServer.Users
                    .FirstOrDefaultAsync(u => u.Name == name && u.Password == password);
                return user;
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it)
                throw new Exception("An error occurred while retrieving the user.", ex);
            }
        }
    }

}
