using DatabaseLibrary.Entities;
using Microsoft.AspNetCore.Mvc;
using Server.ServeRepositories;

namespace Server.Controllers
{
    public class NoteController : Controller
    {

        private NotesRepository notesRepository;


        public NoteController(DbContextServer dbContextServer)
        {
            notesRepository = new NotesRepository(dbContextServer);
        }
        //[HttpPost]
        //public async Task<IActionResult> SaveOrUpdateNote(User user, Note note)
        //{
        //    notesRepository.saveOrUpdateNewNote(user, note);
        //}


    }
}
