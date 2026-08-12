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

        //public async Task<ApiResultData<List<ISyncQueue>>> SendAndReceiveNoteUpdates(List<SyncQueueClient> listChanges, UserClient user)
        //{
        //    try
        //    {
        //        // Construct relative URL with query parameters
        //        string url = _baseURL + "/SyncChanges";

        //        // Serialize notes to JSON and create content
        //        var requestObject = new ```````Request(user, DeviceIdentityService.GetDeviceId(), listChanges);

        //        var json = JsonConvert.SerializeObject(requestObject);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //        // Send POST request with JSON body
        //        var response = await _httpClient.PostAsync(url, content);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            // Parse the LoginResponse from server
        //            var loginResponse = await response.Content.ReadFromJsonAsync<LoginRespons>();
        //            if (loginResponse?.success == true && loginResponse.ChangesToMake != null)
        //            {
        //                return ApiResultData<List<ISyncQueue>>.Success(loginResponse.ChangesToMake);
        //            }
        //            else
        //            {
        //                return ApiResultData<List<ISyncQueue>>.Failure(
        //                    loginResponse?.message ?? "Login failed",
        //                    ApiErrorType.ServerError
        //                );
        //            }
        //        }
        //        else
        //        {
        //            string errorContent = await response.Content.ReadAsStringAsync();
        //            Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
        //            return ApiResultData<List<ISyncQueue>>.Failure($"Server returned error: {response.StatusCode}", ApiErrorType.ServerError);
        //        }
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        Console.WriteLine($"HTTP Error sending notes to server: {ex.Message}");
        //        return ApiResultData<List<ISyncQueue>>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        Console.WriteLine("Request timeout");
        //        return ApiResultData<List<ISyncQueue>>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error sending notes to server: {ex.Message}");
        //        return ApiResultData<List<ISyncQueue>>.Failure($"Error sending notes to server: {ex.Message}", ApiErrorType.Unknown);
        //    }
        //}

        //public async void SaveChangesIfOnline(SyncQueueClient changesMade, Guid DeviceId)
        //{

        //    string url = _baseURL + "SaveOrUpdateNote";
        //    SyncQueueServer changesFromClient = EntityMapper.MapSyncQueueClientToSyncQueueServer(changesMade, DeviceId);
        //    var json = JsonConvert.SerializeObject(changesFromClient);
        //    var content = new StringContent(json, Encoding.UTF8, "application/json");
        //    var response = await _httpClient.PostAsync(url, content);
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        throw new Exception("Failed to save to server" + response.ReasonPhrase);
        //    }
        //}

        //public async Task<ApiResultData<List<NoteClient>>> GetAllNotesFromUser(Guid IdUser)
        //{
        //    try
        //    {
        //        var response = await SendRequest<Object>(HttpMethod.Get, $"{_baseURL}/GetAllNotesFromUser", null);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            // Server returns: { success: true, data: [...notes...] }
        //            // We need to deserialize the wrapper object first
        //            var responseWrapper = await response.Content.ReadFromJsonAsync<NotesResponse>();

        //            if (responseWrapper?.success == true && responseWrapper.data != null)
        //            {
        //                return ApiResultData<List<NoteClient>>.Success(responseWrapper.data);
        //            }
        //            else
        //            {
        //                return ApiResultData<List<NoteClient>>.Failure("No notes found for the user.", ApiErrorType.NotFound);
        //            }
        //        }
        //        else
        //        {
        //            string errorContent = await response.Content.ReadAsStringAsync();
        //            Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
        //            return ApiResultData<List<NoteClient>>.Failure($"Failed to retrieve notes: {response.ReasonPhrase}", ApiErrorType.ServerError);
        //        }
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        Console.WriteLine($"HTTP Error retrieving notes from server: {ex.Message}");
        //        return ApiResultData<List<NoteClient>>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        Console.WriteLine("Request timeout");
        //        return ApiResultData<List<NoteClient>>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error retrieving notes: {ex.Message}");
        //        return ApiResultData<List<NoteClient>>.Failure($"Error retrieving notes: {ex.Message}", ApiErrorType.Unknown);
        //    }
        //}

        public async Task<ApiResultData<List<NoteClient>>> GetAllNotesFromUser(Guid IdUser)
        {
            var result = await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<List<NoteClient>>(
                async () => await SendRequest<Object>(HttpMethod.Get, $"{_baseURL}/GetAllNotesFromUser", null),
                nameof(GetAllNotesFromUser)
            );
            return result;
        }

        //public async Task<ApiResultData<SyncQueueClient>> getNoteChangesFromServer(Guid IdNote)
        //{
        //    try
        //    {
        //        string url = $"{_baseURL}/getServerChangesToNote?IdNode={IdNote}";
        //        var response = await _httpClient.GetAsync(url);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var changesFromServer = response.Content.ReadFromJsonAsync<SyncQueueServer>().Result;
        //            if (changesFromServer != null)
        //            {
        //                var mappedChanges = EntityMapper.MapSyncQueueServerToSyncQueueClient(changesFromServer);
        //                return ApiResultData<SyncQueueClient>.Success(mappedChanges);
        //            }
        //            else
        //            {
        //                return ApiResultData<SyncQueueClient>.Failure("No changes found for the note.", ApiErrorType.NotFound);
        //            }

        //        }
        //        else
        //        {
        //            return ApiResultData<SyncQueueClient>.Failure($"Failed to retrieve changes: {response.ReasonPhrase}", ApiErrorType.ServerError);
        //        }
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        Console.WriteLine($"HTTP Error sending notes to server: {ex.Message}");
        //        return ApiResultData<SyncQueueClient>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        Console.WriteLine("Request timeout");
        //        return ApiResultData<SyncQueueClient>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error during login: {ex.Message}");
        //        return ApiResultData<SyncQueueClient>.Failure($"Error during login: {ex.Message}", ApiErrorType.Unknown);
        //    }
        //}
        //public async Task<ApiResultData<NoteClient>> GetNote(Guid noteId)
        //{
        //    try
        //    {
        //        var response = await SendRequest<Object>(HttpMethod.Get, $"_{_baseURL}/{noteId}", null);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var note = await response.Content.ReadFromJsonAsync<NoteClient>();
        //            return ApiResultData<NoteClient>.Success(note);
        //        }
        //        else
        //        {
        //            string errorContent = await response.Content.ReadAsStringAsync();
        //            Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
        //            return ApiResultData<NoteClient>.Failure($"Server returned error: {response.StatusCode}", ApiErrorType.ServerError);
        //        }
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        return ApiResultData<NoteClient>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        return ApiResultData<NoteClient>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResultData<NoteClient>.Failure($"Error retrieving note: {ex.Message}", ApiErrorType.Unknown);
        //    }
        //}

        public async Task<ApiResultData<NoteClient>> GetNote(Guid noteId)
        {
            return await ExceptionHandlingHelper.ExecuteAsync<NoteClient>(
                async () => await SendRequest<Object>(HttpMethod.Get, $"{_baseURL}/{noteId}", null),
                nameof(GetNote)
            );
        }

        //public async Task<ApiResult> CreateNewNote(NoteClient currentNote)
        //{
        //    try
        //    {
        //        var response = await SendRequest<NoteClient>(HttpMethod.Post, $"{_baseURL}", currentNote);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            // Server returns: { success: true, data: { id: int } }
        //            var noteResponse = await response.Content.ReadFromJsonAsync<CreateNoteResponse>();
        //            if (noteResponse?.success == true)
        //            {
        //                return ApiResult.Success();
        //            }
        //            return ApiResult.Failure("Server returned unexpected response.", ApiErrorType.ServerError);
        //        }
        //        else
        //        {
        //            string errorContent = await response.Content.ReadAsStringAsync();
        //            Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
        //            return ApiResult.Failure($"Server returned error: {response.StatusCode}", ApiErrorType.ServerError);
        //        }
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        return ApiResult.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        return ApiResult.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResult.Failure($"Error creating note: {ex.Message}", ApiErrorType.Unknown);
        //    }
        //}

        public async Task<ApiResult> CreateNewNote(NoteClient currentNote)
        {
            return await ExceptionHandlingHelper.ExecuteAsync(
                async () => await SendRequest<NoteClient>(HttpMethod.Post, $"{_baseURL}", currentNote),
                nameof(CreateNewNote)
            );
        }

        /// <summary>
        /// Updates a note on server with optimistic concurrency control.
        /// Handles 409 Conflict responses by parsing server's current note version.
        /// </summary>
        //public async Task<NoteConflictResult> UpdateNote(NoteClient updatedNote)
        //{
        //    try
        //    {

        //        string url = $"{_baseURL}/{updatedNote.IdNote}";
        //        var response = await SendRequest<NoteClient>(HttpMethod.Put, url, updatedNote);

        //        // Parse the conflict response to get server's current version
        //        string responseContent = await response.Content.ReadAsStringAsync();
        //        Console.WriteLine($"Server response: {responseContent}");

        //        // Try to deserialize the UpdateNoteWithVersionResult from server
        //        var responseData = JsonConvert.DeserializeObject<NoteUpdateResponse>(responseContent);

        //        if (response.IsSuccessStatusCode && responseData?.ServerNote != null)
        //        {
        //            // Success - update went through
        //            if (!responseData.IsVersionConflict)
        //            {
        //                return NoteConflictResult.Success(responseData.ServerNote);
        //            }
        //            else
        //            {
        //                return NoteConflictResult.Conflict(responseData.ServerNote, updatedNote.Version);
        //            }
        //        }

        //        // Other error status
        //        string errorContent = await response.Content.ReadAsStringAsync();
        //        Console.WriteLine($"Failed to update note: {response.StatusCode} - {errorContent}");
        //        return NoteConflictResult.Error(
        //            $"Failed to update note: {response.ReasonPhrase}",
        //            ApiErrorType.ServerError);
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        return NoteConflictResult.Error($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        return NoteConflictResult.Error(
        //            "Request timeout. The server is not responding.",
        //            ApiErrorType.Timeout);
        //    }
        //    catch (Exception ex)
        //    {
        //        return NoteConflictResult.Error($"Error updating note: {ex.Message}", ApiErrorType.Unknown);
        //    }
        //}

        public async Task<ApiResultData<NoteConflictResult>> UpdateNote(NoteClient updatedNote)
        { 
            var result = await ExceptionHandlingHelper.ExecuteAsync<NoteConflictResult>(
                async () => await SendRequest<NoteClient>(HttpMethod.Put, $"{_baseURL}/{updatedNote.IdNote}", updatedNote),
                nameof(UpdateNote)
            );
            return result;
        }

        //public async Task<ApiResult> DeleteNote(Guid noteId)
        //{ 
        //    try
        //    {
        //        var response = await SendRequest<object>(HttpMethod.Delete, $"{_baseURL}/{noteId}", null);
        //        //var response = await _httpClient.DeleteAsync(url);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            return ApiResult.Success();
        //        }
        //        else
        //        {
        //            string errorContent = await response.Content.ReadAsStringAsync();
        //            Console.WriteLine($"Failed to delete note: {response.StatusCode} - {errorContent}");
        //            return ApiResult.Failure($"Failed to delete note: {response.ReasonPhrase}", ApiErrorType.ServerError);
        //        }
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        return ApiResult.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        return ApiResult.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResult.Failure($"Error deleting note: {ex.Message}", ApiErrorType.Unknown);
        //    }
        //}

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
