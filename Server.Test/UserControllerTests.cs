using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Server.Controllers;
using Server.ServeRepositories;
using Xunit;

namespace Server.Test
{
    public class UserControllerTests
    {
        private readonly DbContextServer _dbContext;
        private readonly NotesRepository _repository;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            var options = new DbContextOptionsBuilder<DbContextServer>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new DbContextServer(options);
            _repository = new NotesRepository(_dbContext, null);
            _controller = new UserController(_dbContext, _repository);
        }

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithUserId()
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
                Password = password
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Login(username, password);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var resultObject = okResult.Value as dynamic;
            Assert.True(resultObject.success);
            Assert.Equal(userId, resultObject.IdUser);
        }

        [Fact]
        public async Task Login_WithInvalidUsername_ReturnsUnauthorized()
        {
            // Arrange
            var username = "invaliduser";
            var password = "password123";

            // Act
            var result = await _controller.Login(username, password);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorizedResult.Value);

            var resultObject = unauthorizedResult.Value as dynamic;
            Assert.False(resultObject.success);
            Assert.Contains("Invalid username or password", resultObject.message.ToString());
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var username = "testuser";
            var correctPassword = "correct123";
            var wrongPassword = "wrong123";

            var user = new UserServer
            {
                IdUser = userId,
                Name = "Test User",
                Username = username,
                Password = correctPassword
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Login(username, wrongPassword);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var resultObject = unauthorizedResult.Value as dynamic;
            Assert.False(resultObject.success);
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_WithValidRequest_ReturnsOkWithUserId()
        {
            // Arrange
            var request = new UserController.RegisterRequest
            {
                Name = "New User",
                Username = "newuser",
                Password = "password123"
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var resultObject = okResult.Value as dynamic;
            Assert.True(resultObject.success);
            Assert.NotEqual(Guid.Empty, (Guid)resultObject.IdUser);

            // Verify user was actually created in database
            var createdUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            Assert.NotNull(createdUser);
            Assert.Equal(request.Name, createdUser.Name);
            Assert.Equal(request.Password, createdUser.Password);
        }

        [Fact]
        public async Task Register_WithEmptyUsername_ReturnsBadRequest()
        {
            // Arrange
            var request = new UserController.RegisterRequest
            {
                Name = "New User",
                Username = "",
                Password = "password123"
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var resultObject = badRequestResult.Value as dynamic;
            Assert.False(resultObject.success);
            Assert.Contains("required", resultObject.message.ToString().ToLower());
        }

        [Fact]
        public async Task Register_WithEmptyPassword_ReturnsBadRequest()
        {
            // Arrange
            var request = new UserController.RegisterRequest
            {
                Name = "New User",
                Username = "newuser",
                Password = ""
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var resultObject = badRequestResult.Value as dynamic;
            Assert.False(resultObject.success);
        }

        [Fact]
        public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var username = "existinguser";

            var existingUser = new UserServer
            {
                IdUser = userId,
                Name = "Existing User",
                Username = username,
                Password = "password123"
            };

            _dbContext.Users.Add(existingUser);
            await _dbContext.SaveChangesAsync();

            var request = new UserController.RegisterRequest
            {
                Name = "Another User",
                Username = username,
                Password = "newpassword"
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var resultObject = badRequestResult.Value as dynamic;
            Assert.False(resultObject.success);
            Assert.Contains("already exists", resultObject.message.ToString());
        }

        #endregion
    }
}
