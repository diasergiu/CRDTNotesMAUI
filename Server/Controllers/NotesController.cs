using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.WrapperClasses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Server.ServeRepositories;
using Server.ServerHub;
using System.Linq.Expressions;
using System.Threading.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server.Controllers
{
    [ApiController] // look if you need this
    [Route("api/[controller]")]
    public class NotesController : Controller
    {

        private NotesRepository _notesRepository;
        private DbContextServer _context;


        public NotesController(DbContextServer dbContextServer, NotesRepository notesRepository)
        {
            _context = dbContextServer;
            _notesRepository = notesRepository;
        }
        //[HttpPost("SaveOrUpdateNote")]
        //public async Task SaveOrUpdateNote([FromBody] SyncQueueServer changesMade)
        //{
        //    await _notesRepository.SyncData(changesMade);
        //}

        //[HttpGet("getNotesChangesFromServer")] // iActionResult can sent json back to the client (look more into this)
        //public async Task<IActionResult> getNotesChangesFromServer(Guid IdNote)
        //{
        //    try
        //    {
        //        List<SyncQueueServer> changesServer = _notesRepository.GetChangesFromNote(IdNote);
        //        List<SyncQueueClient> _changesMade = new List<SyncQueueClient>();
        //        foreach (SyncQueueServer syncQueue in changesServer)
        //        {
        //            _changesMade.Add(EntityMapper.MapSyncQueueServerToSyncQueueClient(syncQueue));
        //        }
        //        return Ok(new {
        //            success = true,
        //            message = "Changes synced successfully.",
        //            data = _changesMade
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { success = false, message = $"Error syncing changes: {ex.Message}" });
        //    }

        //}

        [HttpGet("GetAllNotesFromUser")] // iActionResult can sent json back to the client (look more into this)
        public async Task<IActionResult> GetAllNotesFromUser()
        {
            try
            {
                Guid idUser = GetUserIdFromRequest();
                var validationError = ValidateUserId(idUser);
                if (validationError != null)
                {
                    return validationError;
                }
                List<NoteServer> notes = await _notesRepository.GetAllNotesFromUser(idUser);
                List<NoteClient> toSend = new List<NoteClient>();
                foreach (NoteServer note in notes)
                {
                    toSend.Add(EntityMapper.MapNoteServerToNoteClient(note));
                }
                return Ok(new { success = true, data = toSend });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error syncing changes: {ex.Message}" });
            }
        }

        //// PUT /api/notes/{id}
        [HttpPut("{id}")]
        public async Task<UpdateNoteWithVersionResult> UpdateNotes(int noteId, [FromBody] NoteServer note)
        {
            Guid idUser = GetUserIdFromRequest();
            var validationError = ValidateUserId(idUser);
            if (validationError != null)
            {
                //return validationError;
                return UpdateNoteWithVersionResult.Error("error");
            }
            //var updateNoteWithVersionResult =
                return await _notesRepository.UpdateChanges(note, idUser);
            //return Ok(new { success = true, updateNoteWithVersionResult = updateNoteWithVersionResult }); // this needs to be tested


        }

        //// Get /api/notes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNote(Guid noteId)
        {
            Guid idUser = GetUserIdFromRequest();
            var validationError = ValidateUserId(idUser);
            if (validationError != null)
            {
                return validationError;
            }
            NoteServer note = await _notesRepository.GetNoteById(noteId, idUser);
            return Ok(new { success = true, note = note });


        }

        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] NoteClient changesMade)
        {
            try
            {
                Guid idUser = GetUserIdFromRequest();
                var validationError = ValidateUserId(idUser);
                if (validationError != null)
                {
                    return validationError;
                }
                var newNote = await _notesRepository.CreateNote(changesMade, idUser);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
            return Ok(new { success = true });
        }

        [HttpPost("SendChangesToServer")]
        public async Task<IActionResult> SendChangesToServer([FromBody] List<NoteClient> noteClient)
        {
            try { 
                Guid idUser = GetUserIdFromRequest();
                var validationError = ValidateUserId(idUser);
                if (validationError != null)
                {
                    return validationError;
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
            Guid idUser = GetUserIdFromRequest();
            var validationError = ValidateUserId(idUser);
            if (validationError != null)
            {
                return validationError;
            }

            await _notesRepository.DeleteNote(id, idUser);
            return Ok(new { success = true, message = "Note deleted successfully." });
        }

        private Guid GetUserIdFromRequest()
        {
            Guid userId = Guid.Empty;
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
            {
                Guid.TryParse(userIdHeader, out userId);
            }
            return userId;
        }
        private IActionResult ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { success = false, message = "Missing or invalid user ID" });
            }
            return null;
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
