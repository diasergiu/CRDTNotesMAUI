using DatabaseLibrary.Entities;
using Server.ServeRepositories;
using Microsoft.AspNetCore.Mvc;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.ResponsBody;
using Server.Security;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {

        private UserRepository _userRepository;

        public UserController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("login")]
        [HttpGet("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _userRepository.getUser(username, password);
            if (user != null)
            {
                // Return user info as JSON
                var userData = new { idUser = user.IdUser };
                return Ok(ApiResponse<object>.SuccessResponse(userData));
            }
            else
            {
                // Authentication failed, return error as JSON
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Invalid username or password.",
                    "UNAUTHORIZED"
                ));
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Username and password are required.",
                    "INVALID_INPUT"
                ));
            }

            if (request.Username.Length < 3)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Username must be at least 3 characters.",
                    "INVALID_USERNAME_LENGTH"
                ));
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Password must be at least 6 characters.",
                    "INVALID_PASSWORD_LENGTH"
                ));
            }

            // Check if username already exists
            var existingUser = await _userRepository.GetUserByUsername(request.Username);

            if (existingUser != null)
            {
                return Conflict(ApiResponse<object>.ErrorResponse(
                    "Username already exists.",
                    "USERNAME_EXISTS"
                ));
            }

            // Create new user
            var newUser = new UserServer
            {

                Name = request.Name ?? request.Username,
                Username = request.Username,
                Password = PasswordHasher.Hash(request.Password)
            };

            try
            {
                await _userRepository.CreateUser(newUser);

                var userData = new
                {
                    idUser = newUser.IdUser,
                    name = newUser.Name,
                    username = newUser.Username
                };

                return Ok(ApiResponse<object>.SuccessResponse(
                    userData,
                    "Account created successfully."
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    $"Error creating account: {ex.Message}",
                    "SERVER_ERROR"
                ));
            }
        }

        [HttpDelete()]
        public async Task DeleteUserById(Guid idUser)
        {
            await _userRepository.DeleteUser(idUser);
        }

        public class RegisterRequest
        {
            public string Name { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }

}