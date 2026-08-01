using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services
{
    public interface INoteServices
    {
        Task<ApiResultData<List<ISyncQueue>>> SendAndReceiveNoteUpdates(List<SyncQueueClient> listChanges, UserClient user);
        Task<ApiResultData<List<NoteClient>>> GetAllNotesFromUser(Guid IdUser);
        Task<ApiResult> CreateNewNote(NoteClient currentNote);
        Task<NoteConflictResult> UpdateNote(NoteClient updatedNote);
        Task<ApiResult> DeleteNote(Guid noteId);
        Task<ApiResult> SendChangesToServer(List<NoteClient> noteClient);
    }
}
