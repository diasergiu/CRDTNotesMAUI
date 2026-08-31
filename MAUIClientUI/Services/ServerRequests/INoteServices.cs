using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
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
        Task<ApiResultData<List<DToSendChanges>>> GetServerChanges();
        Task<ApiResult> CreateNewNote(NoteClient currentNote);
        Task<ApiResultData<NoteConflictResult>> UpdateNote(NoteClient updatedNote);
        Task<ApiResult> DeleteNote(Guid noteId);
        Task<ApiResult> SendChangesToServer(List<DToSendChanges> noteClient);
        Task<ApiResult> SendCRDTChangestoServer(CRDTChangePayload payload);
        Task<ApiResultData<List<DToSendChanges>>> GetServerChangesByNote(Guid noteId);
        Task<ApiResultData<NoteClient>> GetNote(Guid noteId);
        Task<ApiResult> GiveNoteAccessToUser(Guid noteId, string userId);
    }
}
