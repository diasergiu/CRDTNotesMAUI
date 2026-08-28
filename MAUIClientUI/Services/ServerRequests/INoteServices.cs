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
        Task<ApiResultData<List<NoteClient>>> GetAllNotesFromUser(Guid IdUser);
        Task<ApiResultData<List<CRDTCharacter>>> GetAllCharacterByUser();
        Task<ApiResult> CreateNewNote(NoteClient currentNote);
        Task<ApiResultData<NoteConflictResult>> UpdateNote(NoteClient updatedNote);
        Task<ApiResult> DeleteNote(Guid noteId);
        Task<ApiResult> SendChangesToServer(List<NoteClient> noteClient);
        Task<ApiResult> SendCRDTChangestoServer(List<CRDTCharacter> characters);
        Task<ApiResultData<List<CRDTCharacter>>> GetAllCharacterByNote(Guid noteId);
        Task<ApiResultData<NoteClient>> GetNote(Guid noteId);
        Task<ApiResult> GiveNoteAccessToUser(Guid noteId, Guid userId);
    }
}
