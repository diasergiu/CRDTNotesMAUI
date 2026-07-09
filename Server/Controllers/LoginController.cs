using DatabaseLibrary.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Server.Controllers
{
    public class LoginController : Controller
    {

        private DbContextServer _context;

        public LoginController(DbContextServer context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Login(string username, string password)
        {
            User user = await _context.Users
                .Include(u => u.NotesUsers)
                    .ThenInclude(snu => snu.Note)
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                // Get all notes for the user
                var notes = user.NotesUsers
                    .Select(snu => new
                    {
                        IdNote = snu.Note.IdNote,
                        Title = snu.Note.Title,
                        Content = snu.Note.Content,
                        StartingDate = snu.Note.StartingDate,
                        LastUpdate = snu.Note.LastUpdate
                    })
                    .ToList();

                // Return user info and notes as JSON
                return Json(new
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
                return Json(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }
        }
    }
}
