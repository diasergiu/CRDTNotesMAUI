using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
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

        public async Task<ApiResultData<NoteClient>> GetNote(Guid noteId)
        {
            return await ExceptionHandlingHelper.ExecuteAsync<NoteClient>(
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
            var result = await ExceptionHandlingHelper.ExecuteAsync<NoteConflictResult>(
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

        private class CreateNoteResponse
        {
            public bool success { get; set; }
            public CreateNoteData data { get; set; }
        }

        private class CreateNoteData
        {
            public Guid id { get; set; }
        }

        // Response wrapper for GetAllNotesFromUser endpoint
        // Server returns: { success: true, data: [...] }
        private class NotesResponse
        {
            public bool success { get; set; }
            public List<NoteClient> data { get; set; }
        }

        public class NoteUpdateResponse
        {
            [JsonProperty("isSuccess")]

            public bool IsSuccess { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("serverNote")]
            public NoteServer ServerNote { get; set; }

            [JsonProperty("isVersionConflict")]
            /// <summary>True if version mismatch (concurrency conflict)</summary>
            public bool IsVersionConflict { get; set; }
        }
    }
}
