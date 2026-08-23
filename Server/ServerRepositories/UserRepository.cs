using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.Migrations;
using Microsoft.EntityFrameworkCore;
using Server.Security;
using System;
using System.Threading.Tasks;

namespace Server.ServeRepositories
{
    public class UserRepository
    {
        private DbContextServer _dbContextServer;

        public UserRepository(DbContextServer context)
        {
            _dbContextServer = context;
        }

        public async Task<UserServer> getUser(string username, string password)
        {
            UserServer user = await _dbContextServer.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !PasswordHasher.Verify(password, user.Password))
            {
                return null;
            }

            return user;
        }

        public async Task<UserServer> GetUserByUsername(string username)
        {
            return await _dbContextServer.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<UserServer> CreateUser(UserServer user)
        {
            _dbContextServer.Users.Add(user);
            await _dbContextServer.SaveChangesAsync();
            return user;
        }

        public async Task DeleteUser(Guid idUser)
        {
            _dbContextServer.Users.Remove(new UserServer { IdUser = idUser });
            await _dbContextServer.SaveChangesAsync();
        }
    }
}
