using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using Microsoft.AspNetCore.Mvc;
using Server.ServeRepositories;
using System.Threading.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server.Controllers
{
    [ApiController] // look if you need this
    [Route("api/[controller]")]
    public class NotesController : Controller
    {

        private NotesRepository _notesRepository;


        public NotesController(DbContextServer dbContextServer)
        {
            _notesRepository = new NotesRepository(dbContextServer);
        }
        [HttpPost("SaveOrUpdateNote")]
        public async Task SaveOrUpdateNote([FromBody] SyncQueueServer changesMade)
        {
            await _notesRepository.SyncData(changesMade);
        }

        [HttpGet("getNoteChangesFromServer")] // iActionResult can sent json back to the client (look more into this)
        public async Task<IActionResult> getNoteChangesFromServer(int IdNote)
        {
            try
            {
                List<SyncQueueServer> changesServer = _notesRepository.GetChangesFromNote(IdNote);
                List<SyncQueueClient> _changesMade = new List<SyncQueueClient>();
                foreach (SyncQueueServer syncQueue in changesServer)
                {
                    _changesMade.Add(EntityMapper.MapSyncQueueServerToSyncQueueClient(syncQueue));
                }
                return Ok(new {
                    success = true,
                    message = "Changes synced successfully.",
                    data = _changesMade
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error syncing changes: {ex.Message}" });
            }

        }

        [HttpGet] // iActionResult can sent json back to the client (look more into this)
        public async Task<IActionResult> GetAllNotesFromUser()
        {
            int idUser = -1;

            try
            {
                if (Request.Headers.TryGetValue("X-User-Id", out var idUserHeader))
                {
                    int.TryParse(idUserHeader, out idUser);
                }

                if (idUser == -1)
                {
                    return Unauthorized(new { success = false, message = "Missing user ID" });
                }

                List<NoteServer> notes = await _notesRepository.GetAllNotesFromUser(idUser);
                return Ok(new
                {
                    success = true,
                    message = "Changes synced successfully.",
                    data = notes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error syncing changes: {ex.Message}" });
            }

        }

        //// PUT /api/notes/{id}
        //[HttpPut("{id}")]
        //public async Task UpdateNotes([FromBody] NoteClient Note)
        //{
        //    await _notesRepository.UpdateChanges(changesMade);
        //}

        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] NoteClient changesMade)
        {
            int idUser = -1;
            try
            {
                if (Request.Headers.TryGetValue("X-User-Id", out var idUserHeader))
                {
                    int.TryParse(idUserHeader, out idUser);
                }

                if (idUser == -1)
                {
                    return Unauthorized(new { success = false, message = "Missing user ID" });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
            var newNote = await _notesRepository.CreateNote(changesMade, idUser);
            return Ok(new { success = true, data = new { id = newNote.IdNote } });
  
    }

    private int getUserIdFromRequest()
        {
            return 0;
        }
    }
}
