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

        public UserController(DbContextServer context, NotesRepository notesRepository)
        {
            _context = context;
            _notesRepository = notesRepository;
        }

        [HttpPost("login")]
        [HttpGet("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _notesRepository.getUser(username, password);
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
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

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
                Password = request.Password // TODO: Hash password in production!
            };

            try
            {
                // should put in repository
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

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


        //[HttpPost("SyncChanges")]
        //public async Task<IActionResult> SyncChanges([FromBody] LoginRequest loginBody)
        //{
        //    try
        //    {
        //        UserServer user = EntityMapper.MapUserClientToUserServer(loginBody.user);
        //        List<SyncQueueServer> syncChanges = new List<SyncQueueServer>();
        //        foreach (var change in loginBody.ChangesMade)
        //        {
        //            syncChanges.Add(EntityMapper.MapSyncQueueClientToSyncQueueServer(change, loginBody.IdDevice));
        //        }
        //        // save all changes done offline
        //        await _notesRepository.SyncData(syncChanges);


        //        await _notesRepository.GetServerSyncChanges(loginBody.user, loginBody.IdDevice);

        //        return Ok(new { 
        //            success = true, 
        //            message = "Changes synced successfully.",
        //            data = syncChanges  
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = $"Error syncing changes: {ex.Message}" });
        //    }
        //}

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