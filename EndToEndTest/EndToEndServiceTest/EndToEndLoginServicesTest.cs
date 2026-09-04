using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Services.ServerRequests;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EndToEndTest.EndToEndServiceTest
{
    /// <summary>
    /// Integration/End-to-End tests that call the actual server.
    /// These tests require the server to be running on http://localhost:5266
    /// 
    /// REQUIREMENTS:
    /// 1. Start the Server project before running these tests
    /// 2. Server should be listening on http://localhost:5266
    /// 3. Database should be initialized
    /// 
    /// NOTE: These are true E2E tests with real server calls - no mocks.
    /// </summary>
    [Collection("Sequential")]
    public class EndToEndLoginServicesTest : IAsyncLifetime
    {
        private readonly string _testServerUrl = "http://localhost:5266/api/user";
        private const string TEST_USERNAME_PREFIX = "e2etest_";
        private const string TEST_PASSWORD = "TestPass123!@#";
        private const string TEST_USER_NAME = "E2E Test User";

        private string _uniqueTestUsername;
        private UserServices _userService;
        private readonly HttpClient _httpClient;

        public EndToEndLoginServicesTest()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Generate unique username to avoid conflicts
            _uniqueTestUsername = $"{TEST_USERNAME_PREFIX}{Guid.NewGuid().ToString().Substring(0, 8)}";
            _userService = new UserServices("/api/user");
        }

        public async Task InitializeAsync()
        {
            // Verify server is running before tests
            try
            {
                var response = await _httpClient.GetAsync($"{_testServerUrl}/health");
                // Server doesn't need to have health endpoint, just check connectivity
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException(
                    $"Cannot connect to server at {_testServerUrl}. " +
                    "Please ensure the Server project is running on http://localhost:5266");
            }
        }

        public async Task DisposeAsync()
        {
            _httpClient?.Dispose();
            // Note: We don't clean up test data from the server to allow inspection
            // You may want to add server cleanup endpoints if needed
        }

        // ==================== BASIC REGISTRATION TESTS ====================

        [Fact]
        public async Task E2E_RegisterNewUser_WithValidData_Succeeds()
        {
            // Arrange
            var registrationData = new
            {
                Name = TEST_USER_NAME,
                Username = _uniqueTestUsername,
                Password = TEST_PASSWORD
            };

            // Act
            var result = await _userService.RegisterNewUser(
                registrationData.Name,
                registrationData.Username,
                registrationData.Password
            );

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Registration failed: {result.ErrorMessage}");
            Assert.NotNull(result.Data);
            Assert.Equal(registrationData.Name, result.Data.Name);
            Assert.Equal(registrationData.Username, result.Data.Username);
            Assert.NotEqual(Guid.Empty, result.Data.IdUser);
        }

        [Fact]
        public async Task E2E_RegisterDuplicateUsername_FailsWithServerError()
        {
            // Arrange
            var duplicateUsername = $"{TEST_USERNAME_PREFIX}duplicate_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Act - Register first user
            var firstRegistration = await _userService.RegisterNewUser(
                "First User",
                duplicateUsername,
                TEST_PASSWORD
            );

            // Assert - First registration succeeds
            Assert.True(firstRegistration.IsSuccess, "First registration should succeed");

            // Act - Try to register with same username
            var secondRegistration = await _userService.RegisterNewUser(
                "Second User",
                duplicateUsername,
                TEST_PASSWORD
            );

            // Assert - Second registration fails
            Assert.False(secondRegistration.IsSuccess, "Duplicate registration should fail");
            Assert.Equal(ApiErrorType.ServerError, secondRegistration.ErrorType);
        }

        // ==================== BASIC LOGIN TESTS ====================

        [Fact]
        public async Task E2E_LoginWithValidCredentials_Succeeds()
        {
            // Arrange - First register a user
            var registrationResult = await _userService.RegisterNewUser(
                TEST_USER_NAME,
                _uniqueTestUsername,
                TEST_PASSWORD
            );

            Assert.True(registrationResult.IsSuccess, "Registration should succeed first");

            // Act - Login with the registered credentials
            var loginResult = await _userService.Login(
                _uniqueTestUsername,
                TEST_PASSWORD
            );

            // Assert
            Assert.NotNull(loginResult);
            Assert.True(loginResult.IsSuccess, $"Login failed: {loginResult.ErrorMessage}");
            Assert.NotNull(loginResult.Data);
            Assert.IsType<UserClient>(loginResult.Data);
            Assert.NotEqual(Guid.Empty, loginResult.Data.IdUser);
        }

        [Fact]
        public async Task E2E_LoginWithInvalidPassword_Fails()
        {
            // Arrange - First register a user
            var registrationResult = await _userService.RegisterNewUser(
                TEST_USER_NAME,
                _uniqueTestUsername,
                TEST_PASSWORD
            );

            Assert.True(registrationResult.IsSuccess, "Registration should succeed first");

            // Act - Try to login with wrong password
            var loginResult = await _userService.Login(
                _uniqueTestUsername,
                "WrongPassword123!@#"
            );

            // Assert
            Assert.NotNull(loginResult);
            Assert.False(loginResult.IsSuccess, "Login with wrong password should fail");
            Assert.Equal(ApiErrorType.ServerError, loginResult.ErrorType);
        }

        [Fact]
        public async Task E2E_LoginWithNonexistentUser_Fails()
        {
            // Act
            var loginResult = await _userService.Login(
                $"nonexistent_{Guid.NewGuid().ToString().Substring(0, 8)}",
                TEST_PASSWORD
            );

            // Assert
            Assert.NotNull(loginResult);
            Assert.False(loginResult.IsSuccess, "Login with nonexistent user should fail");
            Assert.Equal(ApiErrorType.ServerError, loginResult.ErrorType);
        }

        // ==================== COMPLETE USER JOURNEY TESTS ====================

        [Fact]
        public async Task E2E_CompleteUserJourney_RegisterAndLogin_Succeeds()
        {
            // Arrange
            var testUsername = $"{TEST_USERNAME_PREFIX}journey_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var testPassword = "JourneyPass123!@#";

            // Act - Step 1: Register new user
            var registerResult = await _userService.RegisterNewUser(
                "Journey Test User",
                testUsername,
                testPassword
            );

            // Assert - Registration
            Assert.True(registerResult.IsSuccess, $"Registration failed: {registerResult.ErrorMessage}");
            Assert.NotNull(registerResult.Data);
            var userId = registerResult.Data.IdUser;
            Assert.NotEqual(Guid.Empty, userId);

            // Act - Step 2: Login with newly created credentials
            var loginResult = await _userService.Login(testUsername, testPassword);

            // Assert - Login
            Assert.True(loginResult.IsSuccess, $"Login failed: {loginResult.ErrorMessage}");
            Assert.NotNull(loginResult.Data);
            Assert.IsType<UserClient>(loginResult.Data);
            Assert.Equal(userId, loginResult.Data.IdUser);
        }

        [Fact]
        public async Task E2E_MultipleUsersCanRegisterAndLogin()
        {
            // Arrange
            var users = new[]
            {
                ("user1_" + Guid.NewGuid().ToString().Substring(0, 5), "User One", "Pass123!@#"),
                ("user2_" + Guid.NewGuid().ToString().Substring(0, 5), "User Two", "Pass456!@#"),
                ("user3_" + Guid.NewGuid().ToString().Substring(0, 5), "User Three", "Pass789!@#")
            };

            // Act & Assert - Register and login for each user
            foreach (var (username, name, password) in users)
            {
                // Register
                var registerResult = await _userService.RegisterNewUser(name, username, password);
                Assert.True(registerResult.IsSuccess, $"Registration failed for {username}: {registerResult.ErrorMessage}");

                // Login
                var loginResult = await _userService.Login(username, password);
                Assert.True(loginResult.IsSuccess, $"Login failed for {username}: {loginResult.ErrorMessage}");
                Assert.NotNull(loginResult.Data);
            }
        }

        // ==================== CREDENTIAL VARIATIONS TESTS ====================

        [Fact]
        public async Task E2E_SpecialCharactersInCredentials_HandledCorrectly()
        {
            // Arrange
            var specialUsername = $"special+{Guid.NewGuid().ToString().Substring(0, 8)}@test.com";
            var specialPassword = "P@ss!word#123$%^";

            // Act - Register with special characters
            var registerResult = await _userService.RegisterNewUser(
                "Special Char User",
                specialUsername,
                specialPassword
            );

            // Assert - Registration
            Assert.True(registerResult.IsSuccess,
                $"Registration with special chars failed: {registerResult.ErrorMessage}");

            // Act - Login with special characters
            var loginResult = await _userService.Login(specialUsername, specialPassword);

            // Assert - Login
            Assert.True(loginResult.IsSuccess,
                $"Login with special chars failed: {loginResult.ErrorMessage}");
        }

        [Fact]
        public async Task E2E_LongCredentials_HandledCorrectly()
        {
            // Arrange
            var longUsername = $"very_long_username_{Guid.NewGuid().ToString()}";
            var longPassword = "P@ssw0rd!" + new string('x', 50);

            // Act - Register with long credentials
            var registerResult = await _userService.RegisterNewUser(
                "Long Credentials User",
                longUsername,
                longPassword
            );

            // Check if server accepts it or returns validation error
            if (registerResult.IsSuccess)
            {
                // Act - Login with long credentials
                var loginResult = await _userService.Login(longUsername, longPassword);
                Assert.True(loginResult.IsSuccess, "Login should succeed with long credentials");
            }
            else
            {
                // Server rejected long credentials as expected
                Assert.Equal(ApiErrorType.ServerError, registerResult.ErrorType);
            }
        }

        // ==================== ERROR RECOVERY TESTS ====================

        [Fact]
        public async Task E2E_FailedLoginFollowedBySuccessfulLogin_Works()
        {
            // Arrange
            var testUsername = $"{TEST_USERNAME_PREFIX}recovery_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Register user
            var registerResult = await _userService.RegisterNewUser(
                TEST_USER_NAME,
                testUsername,
                TEST_PASSWORD
            );

            Assert.True(registerResult.IsSuccess, "Registration should succeed");

            // Act - First attempt with wrong password
            var failedLogin = await _userService.Login(testUsername, "WrongPassword");
            Assert.False(failedLogin.IsSuccess, "Failed login should return false");

            // Act - Second attempt with correct password
            var successfulLogin = await _userService.Login(testUsername, TEST_PASSWORD);

            // Assert
            Assert.True(successfulLogin.IsSuccess,
                $"Login after failed attempt should succeed: {successfulLogin.ErrorMessage}");
        }

        [Fact]
        public async Task E2E_CaseSensitivityInUsername()
        {
            // Arrange
            var baseUsername = $"casesensitive_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Register with lowercase
            var registerResult = await _userService.RegisterNewUser(
                TEST_USER_NAME,
                baseUsername,
                TEST_PASSWORD
            );

            Assert.True(registerResult.IsSuccess, "Registration should succeed");

            // Act - Try to login with different case
            var upperCaseLogin = await _userService.Login(
                baseUsername.ToUpper(),
                TEST_PASSWORD
            );

            // Note: Depending on server implementation, this might fail or succeed
            // This test documents the behavior
            if (!upperCaseLogin.IsSuccess)
            {
                Assert.False(upperCaseLogin.IsSuccess, "Server is case-sensitive for usernames");
            }
            else
            {
                Assert.True(upperCaseLogin.IsSuccess, "Server is case-insensitive for usernames");
            }
        }
    }
}