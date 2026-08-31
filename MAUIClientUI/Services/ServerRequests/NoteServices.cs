using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.ResponsBody;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services.HelperClasses;
using Newtonsoft.Json;
using System;
using System.Net.Http.Json;
using System.Text;

namespace MAUIClientUI.Services.ServerRequests
{
    public class NoteServices : ServicesClient, INoteServices
    {
        private NoteRepository _notesRepository;
        public NoteServices(string URLModifier, NoteRepository noteRepository, IUserContext? userContext = null) 
            : base(URLModifier, userContext)
        {
            _notesRepository = noteRepository;    
        }
        public async Task<ApiResult> SendChangesToServer(List<DToSendChanges> changes)
        {
             return await ExceptionHandlingHelper.ExecuteAsync(
                async () => await SendRequest<List<DToSendChanges>>(HttpMethod.Post, $"{_baseURL}/SendChangesToServer", changes),
                nameof(SendChangesToServer)
            );
        }

        public async Task<ApiResultData<List<NoteClient>>> GetAllNotesFromUser(Guid IdUser)
        {
            var result = await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<List<NoteClient>>(
                async () => await SendRequest<Object>(HttpMethod.Get, $"{_baseURL}/GetAllNotesFromUser", null),
                nameof(GetAllNotesFromUser)
            );
            return result;
        }

        public async Task<ApiResultData<List<DToSendChanges>>> GetServerChanges()
        {
            var result = await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<List<DToSendChanges>>(
                async () => await SendRequest<Object>(HttpMethod.Get, $"{_baseURL}/GetServerChanges", null),
                nameof(GetServerChanges)
            );
            return result;
        }

        public async Task<ApiResultData<NoteClient>> GetNote(Guid noteId)
        {
            return await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<NoteClient>(
                async () => await SendRequest<Object>(HttpMethod.Get, $"{_baseURL}/{noteId}", null, noteId),
                nameof(GetNote)
            );
        }

        public async Task<ApiResult> CreateNewNote(NoteClient currentNote)
        {
            return await ExceptionHandlingHelper.ExecuteAsync(
                async () => await SendRequest<NoteClient>(HttpMethod.Post, $"{_baseURL}", currentNote),
                nameof(CreateNewNote)
            );
        }

        public async Task<ApiResultData<NoteConflictResult>> UpdateNote(NoteClient updatedNote)
        { 
            var result = await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<NoteConflictResult>(
                async () => await SendRequest<NoteClient>(HttpMethod.Put, $"{_baseURL}/{updatedNote.IdNote}", updatedNote, updatedNote.IdNote),
                nameof(UpdateNote)
            );
            return result;
        }   

        public async Task<ApiResult> DeleteNote(Guid noteId)
        {
            return await ExceptionHandlingHelper.ExecuteAsync(
                async () => await SendRequest<object>(HttpMethod.Delete, $"{_baseURL}/{noteId}", null, noteId),
                nameof(DeleteNote)
            );
        }
        public async Task<ApiResult> SendCRDTChangestoServer(CRDTChangePayload payload)
        {
            var result = await ExceptionHandlingHelper.ExecuteAsync(
                async () => await SendRequest<CRDTChangePayload>(
                HttpMethod.Put, $"{_baseURL}/SendCRDTChangestoServer", payload),
                nameof(SendCRDTChangestoServer));

            return result;
        }
        public async Task<ApiResultData<List<DToSendChanges>>> GetServerChangesByNote(Guid noteId)
        {
            var result = await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<List<DToSendChanges>>(
                async () => await SendRequest<Object>(HttpMethod.Get, $"{_baseURL}/GetAllCharacterByNote/{noteId}", null, noteId),
                nameof(GetServerChangesByNote)
            );
            return result;
        }


        public async Task<ApiResult> GiveNoteAccessToUser(Guid noteId, string userName)
        {
            var result = await ExceptionHandlingHelper.ExecuteAsync(
                async () => await SendRequest<object>(HttpMethod.Post, $"{_baseURL}/GiveNoteAccessToUser/{noteId}?userName={userName}", null, noteId),
                nameof(GiveNoteAccessToUser));
            return result;
        }
    }
}
