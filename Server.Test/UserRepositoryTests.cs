using DatabaseLibrary.Entities.Server;
using Microsoft.EntityFrameworkCore;
using Server.Security;
using Server.ServeRepositories;
using Xunit;

namespace Server.Test
{
    public class UserRepositoryTests
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
    }
}
