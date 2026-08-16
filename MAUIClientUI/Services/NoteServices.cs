using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.ResponsBody;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Miscellaneous;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;

namespace MAUIClientUI.Services
{
    public class NoteServices : ServicesClient, INoteServices
    {
        public NoteServices(string URLModifier) : base(URLModifier)
        {
            //_baseURL = 

        }
        public async Task<ApiResult> SendChangesToServer(List<NoteClient> noteClient)
        {
            return await ExceptionHandlingHelper.ExecuteAsync(
                async () => await SendRequest<List<NoteClient>>(HttpMethod.Post, $"{_baseURL}/SendChangesToServer", noteClient),
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

        public async Task<ApiResultData<List<NoteClient>>> GetAllCharacterByUser(Guid IdUser)
        {
            var result = await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<List<NoteClient>>(
                async () => await SendRequest<Object>(HttpMethod.Get, $"{_baseURL}/GetAllNotesFromUser", null),
                nameof(GetAllCharacterByUser)
            );
            return result;
        }

        public async Task<ApiResultData<NoteClient>> GetNote(Guid noteId)
        {
            return await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<NoteClient>(
                async () => await SendRequest<Object>(HttpMethod.Get, $"{_baseURL}/{noteId}", null),
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
                async () => await SendRequest<NoteClient>(HttpMethod.Put, $"{_baseURL}/{updatedNote.IdNote}", updatedNote),
                nameof(UpdateNote)
            );
            return result;
        }   

        public async Task<ApiResult> DeleteNote(Guid noteId)
        {
            return await ExceptionHandlingHelper.ExecuteAsync(
                async () => await SendRequest<object>(HttpMethod.Delete, $"{_baseURL}/{noteId}", null),
                nameof(DeleteNote)
            );
        }
        public async Task<ApiResult> SendCRDTChangestoServer(List<CRDTCharacter> characters)
        {
            var result = await ExceptionHandlingHelper.ExecuteAsync(
                async () => await SendRequest<List<CRDTCharacter>>(
                HttpMethod.Put, $"{_baseURL}/SendCRDTChangestoServer", characters),
                nameof(SendCRDTChangestoServer));
            return result;
        }
        public async Task<ApiResultData<List<CRDTCharacter>>> GetAllCharacterByUser()
        {
            var result = await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<List<CRDTCharacter>>
                (async () => await SendRequest<object>
            (HttpMethod.Get, $"{_baseURL}/GetServerChanges", null),
                nameof(GetAllCharacterByUser));
            return result;

        }
        private HttpRequestMessage GetRequestWithHIdHeader(HttpMethod method ,string url) {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-User-Id", UserDevice.LocalUser.ToString());
            return request;
        }
        private async Task<HttpResponseMessage> SendRequest<T>(HttpMethod method, String url, T? data)
        {
            var request = GetRequestWithHIdHeader(method, url);
            if (data != null)
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var json = JsonConvert.SerializeObject(data, settings);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            var result = await _httpClient.SendAsync(request);
            return result;
        }

       
    }
}
