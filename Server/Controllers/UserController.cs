using DatabaseLibrary.Entities;
using Server.ServeRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DatabaseLibrary.RequestBody;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.ResponsBody;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {

        private DbContextServer _context; // make a repository for user later
        private NotesRepository _notesRepository;

        public UserController(DbContextServer context)
        {
            _context = context;
            _notesRepository = new NotesRepository(context);
        }

        [HttpPost("login")]
        [HttpGet("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _notesRepository.getUser(username, password);
            if (user != null)
            {
                // Return user info and notes as JSON
                return Ok(new
                {
                    success = true,
                    IdUser = user.IdUser,
                });
            }
            else
            {
                // Authentication failed, return error as JSON
                return Unauthorized(new
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


        [HttpPost("SyncChanges")]
        public async Task<IActionResult> SyncChanges([FromBody] LoginRequest loginBody)
        {
            try
            {
                UserServer user = EntityMapper.MapUserClientToUserServer(loginBody.user);
                List<SyncQueueServer> syncChanges = new List<SyncQueueServer>();
                foreach (var change in loginBody.ChangesMade)
                {
                    syncChanges.Add(EntityMapper.MapSyncQueueClientToSyncQueueServer(change, loginBody.IdDevice));
                }
                // save all changes done offline
                await _notesRepository.SyncData(syncChanges);


                await _notesRepository.GetServerSyncChanges(loginBody.user, loginBody.IdDevice);

                return Ok(new { 
                    success = true, 
                    message = "Changes synced successfully.",
                    data = syncChanges  
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error syncing changes: {ex.Message}" });
            }
        }

        [HttpDelete()]
        public async Task DeleteUserById(Guid idUser)
        {
            _context.Users.Remove(new UserServer { IdUser = idUser });
            await _context.SaveChangesAsync();
        }

        public class RegisterRequest
        {
            public string Name { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }

}