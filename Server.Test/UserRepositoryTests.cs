using DatabaseLibrary.Entities.Server;
using Microsoft.EntityFrameworkCore;
using Server.Security;
using Server.ServeRepositories;
using Xunit;

namespace Server.Test
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly DbContextServer _dbContext;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DbContextServer>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new DbContextServer(options);
            _repository = new UserRepository(_dbContext);
        }

        public void Dispose()
        {
            try
            {
                _dbContext.Database.EnsureDeleted();
            }
            catch
            {
                // Best-effort cleanup.
            }
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetUser_WithValidCredentials_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var username = "testuser";
            var password = "password123";

            var user = new UserServer
            {
                IdUser = userId,
                Name = "Test User",
                Username = username,
                Password = PasswordHasher.Hash(password)
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.getUser(username, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.IdUser);
            Assert.Equal(username, result.Username);
        }

        [Fact]
        public async Task GetUser_WithInvalidUsername_ReturnsNull()
        {
            // Arrange
            var username = "nonexistentuser";
            var password = "password123";

            // Act
            var result = await _repository.getUser(username, password);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUser_WithInvalidPassword_ReturnsNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var username = "testuser";
            var password = "password123";

            var user = new UserServer
            {
                IdUser = userId,
                Name = "Test User",
                Username = username,
                Password = PasswordHasher.Hash(password)
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.getUser(username, "wrongpassword");

            // Assert
            Assert.Null(result);
        }

        #region SEC-08: Password Hashing

        [Fact]
        public async Task CreateUser_PasswordIsHashedNotPlaintext()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var username = "testuser";
            var plainPassword = "MyPlainPassword123!";

            var user = new UserServer
            {
                IdUser = userId,
                Name = "Test User",
                Username = username,
                Password = PasswordHasher.Hash(plainPassword)
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act: Retrieve the user record directly from the database
            var userFromDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.IdUser == userId);

            // Assert
            Assert.NotNull(userFromDb);
            Assert.NotNull(userFromDb.Password);

            // Password must NOT be the plaintext original
            Assert.NotEqual(plainPassword, userFromDb.Password);

            // Password must contain the structure of a hashed password (iterations.salt.hash)
            var hashParts = userFromDb.Password.Split('.');
            Assert.Equal(3, hashParts.Length);
            Assert.True(int.TryParse(hashParts[0], out var iterations));
            Assert.True(iterations > 0);

            // Salt and hash should be non-empty base64-encoded strings
            Assert.NotEmpty(hashParts[1]); // salt
            Assert.NotEmpty(hashParts[2]); // hash

            // Verify the hash can be used to validate the original password
            Assert.True(PasswordHasher.Verify(plainPassword, userFromDb.Password));

            // Verify the hash does NOT match an incorrect password
            Assert.False(PasswordHasher.Verify("WrongPassword", userFromDb.Password));
        }

        #endregion
    }
}
