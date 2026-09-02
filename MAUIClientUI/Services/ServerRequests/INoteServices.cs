using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Services.HelperClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services
{
    public interface INoteServices
    {
        Task<ApiResultData<List<NoteClient>>> GetAllNotesFromUser(Guid IdUser);
        Task<ApiResultData<List<NoteServer>>> GetServerChanges();
        Task<ApiResult> CreateNewNote(NoteClient currentNote);
        Task<ApiResultData<NoteConflictResult>> UpdateNote(NoteClient updatedNote);
        Task<ApiResult> DeleteNote(Guid noteId);
        Task<ApiResultData<List<NoteServer>>> SendChangesToServer(List<DTOSendChanges> noteClient);
        Task<ApiResult> SendCRDTChangestoServer(CRDTChangePayload payload);
        Task<ApiResultData<List<DTOSendChanges>>> GetServerChangesByNote(Guid noteId);
        Task<ApiResultData<NoteClient>> GetNote(Guid noteId);
        Task<ApiResult> GiveNoteAccessToUser(Guid noteId, string userId);
    }
}
