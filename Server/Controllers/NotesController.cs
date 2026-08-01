using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.WrapperClasses;
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
        public async Task<IActionResult> getNoteChangesFromServer(Guid IdNote)
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

        [HttpGet("GetAllNotesFromUser")] // iActionResult can sent json back to the client (look more into this)
        public async Task<IActionResult> GetAllNotesFromUser(Guid IdUser)
        {
            try
            {
                List<NoteServer> notes = await _notesRepository.GetAllNotesFromUser(IdUser);
                List<NoteClient> toSend = new List<NoteClient>();
                foreach(NoteServer note in notes)
                {
                    toSend.Add(EntityMapper.MapNoteServerToNoteClient(note));
                }
                return Ok(new { success = true, data = toSend });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error syncing changes: {ex.Message}" });
            }

            //try
            //{
            //    if (Request.Headers.TryGetValue("X-User-Id", out var idUserHeader))
            //    {
            //        int.TryParse(idUserHeader, out idUser);
            //    }

            //    if (idUser == -1)
            //    {
            //        return Unauthorized(new { success = false, message = "Missing user ID" });
            //    }

            //    List<NoteServer> notes = await _notesRepository.GetAllNotesFromUser(idUser);
            //    return Ok(new
            //    {
            //        success = true,
            //        message = "Changes synced successfully.",
            //        data = notes
            //    });
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new { success = false, message = $"Error syncing changes: {ex.Message}" });
            //}

        }

        //// PUT /api/notes/{id}
        [HttpPut("{id}")]
        public async Task<UpdateNoteWithVersionResult> UpdateNotes(int noteId, [FromBody] NoteServer note)
        {
            return await _notesRepository.UpdateChanges(note);

        }

        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] NoteClient changesMade)
        {
            Guid idUser = Guid.Empty;
            try
            {
                if (Request.Headers.TryGetValue("X-User-Id", out var idUserHeader))
                {
                    Guid.TryParse(idUserHeader, out idUser);
                }

                if (idUser == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "Missing user ID" });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
            var newNote = await _notesRepository.CreateNote(changesMade, idUser);
            return Ok(new { success = true });

    }

        [HttpPost("SendChangesToServer")]
        public async Task<IActionResult> SendChangesToServer([FromBody] List<NoteClient> noteClient)
        {
            try
            {
                Guid idUser = Guid.Empty;

                if (Request.Headers.TryGetValue("X-User-Id", out var idUserHeader))
                {
                    Guid.TryParse(idUserHeader, out idUser);
                }

                if (idUser == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "Missing user ID" });
                }

                await _notesRepository.SaveAllChangesFromClient(ConvertListClientToServer(noteClient), idUser);
                return Ok(new { success = true, message = "Changes synced successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error syncing changes: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(Guid id)
        {
            try
            {
                Guid idUser = Guid.Empty;

                if (Request.Headers.TryGetValue("X-User-Id", out var idUserHeader))
                {
                    Guid.TryParse(idUserHeader, out idUser);
                }

                if (idUser == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "Missing user ID" });
                }

                await _notesRepository.DeleteNote(id, idUser);
                return Ok(new { success = true, message = "Note deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error deleting note: {ex.Message}" });
            }
        }

        private int getUserIdFromRequest()
        {
            return 0;
        }

        private List<NoteServer> ConvertListClientToServer( List<NoteClient> list)
        {
            List<NoteServer> newList = new List<NoteServer>();
            foreach(NoteClient note in list)
            {
                newList.Add(EntityMapper.MapNoteClientToNoteServer(note));
            }
            return newList;
        }
    }
}
