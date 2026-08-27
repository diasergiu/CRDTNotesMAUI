using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.ResponsBody;
using DatabaseLibrary.WrapperClasses;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("GetAllNotesFromUser")] // iActionResult can sent json back to the client (look more into this)
        public async Task<IActionResult> GetAllNotesFromUser()
        {
            Guid idUser = GetUserIdFromRequest();
            List<NoteClient> notes = await GetListOfNotesByUser(idUser);
            return Ok(ApiResponse<List<NoteClient>>.SuccessResponse(notes));

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
        [HttpGet("GetAllNotesDTOFromUser")]
        public async Task<IActionResult> GetAllNotesDTOFromUser()
        {
            Guid idUser = GetUserIdFromRequest();
            var CRDTCharacters = await _notesRepository.GetAllCRDTByUser(idUser);
            var notes = await GetListOfNotesByUser(idUser);
            NotesDTO toSend = new NotesDTO
            {
                NoteClient = notes,
                CRDTCharacter = CRDTCharacters
            };  
            return Ok(ApiResponse<NotesDTO>.SuccessResponse(toSend));
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
            if(!_notesRepository.DoseUserHaveAccessToNote(id, idUser))
            {
                return BadRequest("You do not have access to this note.");
            }
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

        private async Task<List<NoteClient>> GetListOfNotesByUser(Guid userId)
        {
            List<NoteServer> notes = await _notesRepository.GetAllNotesFromUser(userId);
            return notes
                .Select(note => EntityMapper.MapNoteServerToNoteClient(note))
                .ToList();
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
            if(_notesRepository.DoseUserHaveAccessToNote(noteId, userId) == false)
            {
                return BadRequest("You do not have access to this note.");
            }
            _notesRepository.SaveNoteUserConnection(noteId, userId);
            var characters = await _notesRepository.getCRDTCharactersbyIdNote(noteId, userId);
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