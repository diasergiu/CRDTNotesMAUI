using DatabaseLibrary.Entities;
using Server.ServerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Server.Controllers
{
    public class LoginController : Controller
    {

        //private DbContextServer _context;
        private NotesService _notesService;

        public LoginController(DbContextServer context)
        {
            //_context = context;
            _notesService = new NotesService(context);
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Login(string username, string password, List<Note> listNewNotes)
        {
            var user = await _notesService.getUser(username, password);
            if (user != null)
            {
                // save all changes done offline
                await _notesService.SaveOrUpdateNotes(user, listNewNotes);

                // Get all notes for the user
                var notes = await _notesService.GetNotesToUpdateClient(user);

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
