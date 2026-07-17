using DatabaseLibrary.Entities;
using Server.ServeRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DatabaseLibrary.RequestBody;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {

        private DbContextServer _context;
        private NotesRepository _notesService;

        public UserController(DbContextServer context)
        {
            _context = context;
            _notesService = new NotesRepository(context);
        }

        [HttpPost("login")]
        [HttpGet("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginBody)
        {
            var user = await _notesService.getUser(loginBody.Username, loginBody.Password);
            if (user != null)
            {
                // save all changes done offline
                await _notesService.SaveOrUpdateNotes(user, loginBody.OfflineNotes ?? new List<Note>());
         
                // Get all notes for the user
                var notes = await _notesService.GetNotesToUpdateClient(user);

                // Return user info and notes as JSON
                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        IdUser = user.IdUser,
                        Name = user.Name,
                        Username = user.Username
                    },
                    notes = notes
                });
            }
            else
            {
                // Authentication failed, return error as JSON
                return Ok(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }
        }

        [HttpPost("register")]
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
            var newUser = new UserServer
            {
                Name = request.Name ?? request.Username,
                Username = request.Username,
                Password = request.Password // TODO: Hash password in production!
            };

            try
            {
                // should put in repository
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
