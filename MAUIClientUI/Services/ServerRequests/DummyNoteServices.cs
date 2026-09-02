using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Services.HelperClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services
{
    public class DummyNoteServices : INoteServices
    {
        public async Task<ApiResultData<List<NoteServer>>> SendChangesToServer(List<DTOSendChanges> noteClient)
        {
            return ApiResultData<List<NoteServer>>.Success(new List<NoteServer>());
        }

        public async Task<ApiResultData<List<NoteClient>>> GetAllNotesFromUser(Guid IdUser)
        {
            return ApiResultData<List<NoteClient>>.Success(new List<NoteClient>());
        }

        public async Task<ApiResultData<List<NoteServer>>> GetServerChanges()
        {
            return ApiResultData<List<NoteServer>>.Success(new List<NoteServer>());
        }

        public async Task<ApiResult> CreateNewNote(NoteClient currentNote)
        {
            return ApiResult.Success();
        }

        public async Task<ApiResultData<NoteConflictResult>> UpdateNote(NoteClient updatedNote)
        {
            return ApiResultData<NoteConflictResult>.Success(NoteConflictResult.Success(EntityMapper.MapNoteClientToNoteServer(updatedNote)));
        }

        public async Task<ApiResult> DeleteNote(Guid noteId)
        {
            return ApiResult.Success();
        }

        public async Task<ApiResult> SendCRDTChangestoServer(CRDTChangePayload payload)
        {
            return ApiResult.Success();
        }

        public async Task<ApiResultData<List<DTOSendChanges>>> GetServerChangesByNote(Guid noteId)
        {
            return ApiResultData<List<DTOSendChanges>>.Success(new List<DTOSendChanges>());
        }

        public async Task<ApiResultData<NoteClient>> GetNote(Guid noteId)
        {
            return ApiResultData<NoteClient>.Success(new NoteClient());
        }

        public async Task<ApiResult> GiveNoteAccessToUser(Guid noteId, string userName)
        {
            return ApiResult.Success();
        }
    }
}
