using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.ResponsBody;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Server.Controllers;
using Server.Security;
using Server.ServeRepositories;
using Server.ServerHub;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Server.Test
{
    public class UserControllerTests : IDisposable
    {
        private readonly DbContextServer _dbContext;
        private readonly UserRepository _repository;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            var options = new DbContextOptionsBuilder<DbContextServer>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new DbContextServer(options);


            _repository = new UserRepository(_dbContext);
            _controller = new UserController(_repository);
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
                Password = PasswordHasher.Hash(password)
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Login(username, password);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var apiResponse = okResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);

            // The Data should be an anonymous object with idUser property
            Assert.NotNull(apiResponse.Data);
            // Convert to JsonElement and access the property
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(apiResponse.Data));
            jsonElement.TryGetProperty("idUser", out var idUserElement);
            Guid returnedUserId = Guid.Parse(idUserElement.GetString());

            Assert.Equal(userId, returnedUserId);
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

            var apiResponse = unauthorizedResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Contains("Invalid username or password", apiResponse.Message);
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
                Password = PasswordHasher.Hash(correctPassword)
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Login(username, wrongPassword);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var apiResponse = unauthorizedResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
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

            var apiResponse = okResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);

            // Extract IdUser from the anonymous Data object using reflection
            var dataProperty = apiResponse.Data?.GetType().GetProperty("idUser", System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public);
            var idUserValue = dataProperty?.GetValue(apiResponse.Data) as Guid?;
            Assert.NotEqual(Guid.Empty, idUserValue);

            // Verify user was actually created in database
            var createdUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            Assert.NotNull(createdUser);
            Assert.Equal(request.Name, createdUser.Name);
            Assert.NotEqual(request.Password, createdUser.Password);
            Assert.True(PasswordHasher.Verify(request.Password, createdUser.Password));
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
            var apiResponse = badRequestResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Contains("required", apiResponse.Message.ToLower());
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
            var apiResponse = badRequestResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
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
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var apiResponse = conflictResult.Value as ApiResponse<object>;
            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Contains("already exists", apiResponse.Message);
        }

        #endregion
    }
}
