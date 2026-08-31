using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.ResponsBody;
using DatabaseLibrary.WrapperClasses;
using Microsoft.AspNetCore.Mvc;
using Server.Filters;
using Server.ServeRepositories;


namespace Server.Controllers
{
    [ApiController] // look if you need this
    [Route("api/[controller]")]
    public class NotesController : Controller
    {

        private NotesRepository _notesRepository;
        private NoteSyncHub _noteSyncHub;


        public NotesController(NotesRepository notesRepository, NoteSyncHub noteSyncHub)
        {
            _notesRepository = notesRepository;
            _noteSyncHub = noteSyncHub;
        }

        [HttpGet("GetAllNotesFromUser")] 
        public async Task<IActionResult> GetAllNotesFromUser()
        {
            Guid idUser = GetUserIdFromRequest();
            List<NoteClient> notes = await GetListOfNotesByUser(idUser);
            return Ok(ApiResponse<List<NoteClient>>.SuccessResponse(notes));

        }
        //// Get /api/notes/{id}
        [HttpGet("{id}")]
        [NoteAccessAuthorization("noteId")]
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


        [HttpGet("GetServerChanges")]
        public async Task<IActionResult> GetServerChanges()
        {
            Guid userId = GetUserIdFromRequest();
            var serverChanges = await _notesRepository.GetAllCRDTByUser(userId);

            // Convert CRDTCharacterServer to DToSendChanges for client
            var dtoChanges = serverChanges.Select(sc => new DToSendChanges
            {
                NoteServer = sc.NoteServer,
                Payload = sc.Payload
            }).ToList();

            return Ok(ApiResponse<object>.SuccessResponse(
                data: dtoChanges,
                message: "Server changes retrieved successfully."
            ));
        }
        [HttpGet("GetAllCharacterByNote")]
        [NoteAccessAuthorization("noteId")]
        public async Task<IActionResult> GetAllCharacterByNote(Guid noteId)
        {
            Guid userId = GetUserIdFromRequest();
            await _notesRepository.SaveNoteUserConnection(noteId, userId);
            var serverChanges = await _notesRepository.getCRDTCharactersbyIdNote(noteId, userId);

            // Convert CRDTCharacterServer to DToSendChanges for client
            var dtoChanges = serverChanges.Select(sc => new DToSendChanges
            {
                NoteServer = sc.NoteServer,
                Payload = sc.Payload
            }).ToList();

            return Ok(ApiResponse<object>.SuccessResponse(
                data: dtoChanges,
                message: "Characters retrieved successfully."
            ));
        }
        [HttpPut("SendCRDTChangestoServer")]
        public async Task<IActionResult> SendCRDTChangestoServer([FromBody] CRDTChangePayload changes)
        {
            Guid idUser = GetUserIdFromRequest();
            await _notesRepository.saveCRDTChanges(changes);
            string? senderConnectionId = Request.Headers["X-Connection-Id"].FirstOrDefault();
            await _noteSyncHub.PushUpdatesToSubscribedUserAsync(changes, idUser, senderConnectionId);

            return Ok("Success");
        }

        //// PUT /api/notes/{id}
        [HttpPut("{id}")]
        [NoteAccessAuthorization("noteId")]
        public async Task<IActionResult> UpdateNotes(Guid noteId, [FromBody] NoteClient note)
        {
            Guid idUser = GetUserIdFromRequest();
            var updateNoteWithVersionResult = await _notesRepository.UpdateChanges(EntityMapper.MapNoteClientToNoteServer(note), idUser);
            return Ok(ApiResponse<object>.SuccessResponse(updateNoteWithVersionResult));
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
        public async Task<IActionResult> SendChangesToServer([FromBody] List<DToSendChanges> noteClient)
        {
            Guid idUser = GetUserIdFromRequest();
            await _notesRepository.SaveAllChangesFromClient(noteClient, idUser);
            return Ok(ApiResponse<object>.SuccessResponse(
                data: null,
                message: "Changes synced successfully."
            ));

        }
        [HttpPost("GiveNoteAccessToUser/{noteId}")]
        [NoteAccessAuthorization("noteId")]
        public async Task<IActionResult> GiveNoteAccessToUser(Guid noteId, [FromQuery] string UserName)
        {
            Guid userId = GetUserIdFromRequest();
            await _notesRepository.SaveNoteUserConnection(noteId, UserName);
            return Ok(ApiResponse<object>.SuccessResponse(
                data: null,
                message: "Note access granted successfully."
            ));
        }

        [HttpDelete("{id}")]
        [NoteAccessAuthorization("id")]
        public async Task<IActionResult> DeleteNote(Guid id)
        {
            Guid userId = GetUserIdFromRequest();
            await _notesRepository.DeleteNote(id, userId);
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

        private async Task<List<NoteClient>> GetListOfNotesByUser(Guid userId)
        {
            List<NoteServer> notes = await _notesRepository.GetAllNotesFromUser(userId);
            return notes
                .Select(note => EntityMapper.MapNoteServerToNoteClient(note))
                .ToList();
        }
    }
}