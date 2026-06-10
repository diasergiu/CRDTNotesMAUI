using DatabaseLibrary.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly DbContextServer _context;

        public RegisterController(DbContextServer context)
        {
            _context = context;
        }

    
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username and password are required."
                });
            }

            if (request.Username.Length < 3)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username must be at least 3 characters."
                });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password must be at least 6 characters."
                });
            }

            // Check if username already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (existingUser != null)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Username already exists."
                });
            }

            // Create new user
            var newUser = new User
            {
                Name = request.Name ?? request.Username,
                Username = request.Username,
                Password = request.Password // TODO: Hash password in production!
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Account created successfully.",
                    user = new
                    {
                        idUser = newUser.IdUser,
                        name = newUser.Name,
                        username = newUser.Username
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error creating account: {ex.Message}"
                });
            }
        }
    }

    public class RegisterRequest
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

}
