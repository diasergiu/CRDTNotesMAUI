using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.ResponsBody;
using DatabaseLibrary.WrapperClasses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
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
        private NoteSyncHub _noteSyncHub;


        public NotesController(DbContextServer dbContextServer, NotesRepository notesRepository, NoteSyncHub noteSyncHub)
        {
            _context = dbContextServer;
            _notesRepository = notesRepository;
            _noteSyncHub = noteSyncHub;
        }

        [HttpGet("GetAllNotesFromUser")] // iActionResult can sent json back to the client (look more into this)
        public async Task<IActionResult> GetAllNotesFromUser()
        {

            Guid idUser = GetUserIdFromRequest();
            List<NoteServer> notes = await _notesRepository.GetAllNotesFromUser(idUser);
            List<NoteClient> toSend = notes
                .Select(note => EntityMapper.MapNoteServerToNoteClient(note))
                .ToList();
            return Ok(ApiResponse<List<NoteClient>>.SuccessResponse(toSend));

        }

        [HttpPut("SendCRDTChangestoServer")]
        public async Task<IActionResult> SendCRDTChangestoServer([FromBody] List<CRDTCharacter> changes)
        {
            Guid idUser = GetUserIdFromRequest();
            await _notesRepository.saveCRDTChanges(changes);
            string? senderConnectionId = Request.Headers["X-Connection-Id"].FirstOrDefault();
            await _noteSyncHub.PushUpdatesToSubscribedUserAsync(changes, idUser, senderConnectionId);

            return Ok("Success");
        }

        //// PUT /api/notes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNotes(Guid noteId, [FromBody] NoteClient note)
        {
            Guid idUser = GetUserIdFromRequest();
            var updateNoteWithVersionResult = await _notesRepository.UpdateChanges(EntityMapper.MapNoteClientToNoteServer(note), idUser);
            return Ok(ApiResponse<object>.SuccessResponse(updateNoteWithVersionResult));
        }

        //// Get /api/notes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNote(Guid noteId)
        {
            Guid idUser = GetUserIdFromRequest();
            NoteServer note = await _notesRepository.GetNoteById(noteId, idUser);
            if (note == null)
            {
                return BadRequest("Note not found or you do not have access to it.");
            }
            return Ok(ApiResponse<object>.SuccessResponse(note));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] NoteClient changesMade)
        {

            Guid idUser = GetUserIdFromRequest();
            var newNote = await _notesRepository.CreateNote(EntityMapper.MapNoteClientToNoteServer(changesMade), idUser);
            var noteData = new { idNote = newNote.IdNote };
            return Ok(ApiResponse<object>.SuccessResponse(
                noteData,
                "Note created successfully."
            ));

        }

        [HttpPost("SendChangesToServer")]
        public async Task<IActionResult> SendChangesToServer([FromBody] List<NoteClient> noteClient)
        {

            Guid idUser = GetUserIdFromRequest();
            await _notesRepository.SaveAllChangesFromClient(ConvertListClientToServer(noteClient), idUser);
            return Ok(ApiResponse<object>.SuccessResponse(
                data: null,
                message: "Changes synced successfully."
            ));

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(Guid id)
        {
            Guid idUser = GetUserIdFromRequest();
            await _notesRepository.DeleteNote(id, idUser);
            return Ok(ApiResponse<object>.SuccessResponse(
                data: null,
                message: "Note deleted successfully."
            ));
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
        private List<NoteServer> ConvertListClientToServer(List<NoteClient> list)
        {
            List<NoteServer> newList = new List<NoteServer>();
            foreach (NoteClient note in list)
            {
                newList.Add(EntityMapper.MapNoteClientToNoteServer(note));
            }
            return newList;
        }


        [HttpGet("GetServerChanges")]
        public async Task<IActionResult> GetServerChanges()
        {
            Guid userId = GetUserIdFromRequest();
            var changes = await _notesRepository.GetAllCRDTByUser(userId);
            return Ok(ApiResponse<object>.SuccessResponse(
                data: changes,
                message: "Server changes retrieved successfully."
            ));
        }


        [HttpGet("GetAllCharacterByNote")]
        public async Task<IActionResult> GetAllCharacterByNote(Guid noteId)
        {
            Guid userId = GetUserIdFromRequest();
            _notesRepository.SaveNoteUserConnection(noteId, userId);
            var characters = await _notesRepository.getCRDTCharactersbyIdNote(noteId);
            return Ok(ApiResponse<object>.SuccessResponse(
                data: characters,
                message: "Characters retrieved successfully."
            ));
        }

        [HttpPost("GiveNoteAccessToUser")]
        public async Task<IActionResult> GiveNoteAccessToUser(Guid userId, Guid noteId)
        {
            //Guid userId = GetUserIdFromRequest();
            await _notesRepository.SaveNoteUserConnection(noteId, userId);
            return Ok(ApiResponse<object>.SuccessResponse(
                data: null,
                message: "Note access granted successfully."
            ));
        }
    }
}