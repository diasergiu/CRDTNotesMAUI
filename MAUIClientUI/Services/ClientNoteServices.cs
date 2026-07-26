using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.ResponsBody;
using DatabaseLibrary.WrapperClasses;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SlackAPI;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MAUIClientUI.Services
{
    public class ClientNoteServices : ServicesClient
    {
        public ClientNoteServices(string URLModifier) : base(URLModifier)
        {

        }

        public async Task<ApiResult<List<ISyncQueue>>> SendAndReceiveNoteUpdates(List<SyncQueueClient> listChanges, UserClient user)
        {
            try
            {
                // Construct relative URL with query parameters
                string url = _baseURL + "/SyncChanges";

                // Serialize notes to JSON and create content
                var requestObject = new LoginRequest(user, DeviceIdentityService.GetDeviceId(), listChanges);

                var json = JsonConvert.SerializeObject(requestObject);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send POST request with JSON body
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    // Parse the LoginResponse from server
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginRespons>();
                    if (loginResponse?.success == true && loginResponse.ChangesToMake != null)
                    {
                        return ApiResult<List<ISyncQueue>>.Success(loginResponse.ChangesToMake);
                    }
                    else
                    {
                        return ApiResult<List<ISyncQueue>>.Failure(
                            loginResponse?.message ?? "Login failed",
                            ApiErrorType.ServerError
                        );
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
                    return ApiResult<List<ISyncQueue>>.Failure($"Server returned error: {response.StatusCode}", ApiErrorType.ServerError);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error sending notes to server: {ex.Message}");
                return ApiResult<List<ISyncQueue>>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timeout");
                return ApiResult<List<ISyncQueue>>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notes to server: {ex.Message}");
                return ApiResult<List<ISyncQueue>>.Failure($"Error sending notes to server: {ex.Message}", ApiErrorType.Unknown);
            }
        }
        // untested if it workes
        public ApiResult<UserClient> Login(string username, string password)
        {
            try
            {
                string url = $"{_baseURL}/login?username={username}&password={password}";

                var response = _httpClient.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    var user = response.Content.ReadFromJsonAsync<UserClient>().Result;
                    return ApiResult<UserClient>.Success(user);
                }
                else
                {
                    return ApiResult<UserClient>.Failure($"Login failed: {response.ReasonPhrase}", ApiErrorType.ServerError);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error sending notes to server: {ex.Message}");
                return ApiResult<UserClient>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timeout");
                return ApiResult<UserClient>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return ApiResult<UserClient>.Failure($"Error during login: {ex.Message}", ApiErrorType.Unknown);
            }
        }

        public async void SaveChangesIfOnline(SyncQueueClient changesMade, int DeviceId)
        {

            string url = _baseURL + "SaveOrUpdateNote";
            SyncQueueServer changesFromClient = EntityMapper.MapSyncQueueClientToSyncQueueServer(changesMade, DeviceId);
            var json = JsonConvert.SerializeObject(changesFromClient);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to save to server" + response.ReasonPhrase);
            }
        }


        public async Task<ApiResult<SyncQueueClient>> getNoteChangesFromServer(int IdNote)
        {
            try
            {
                string url = $"{_baseURL}/getServerChangesToNote?IdNode={IdNote}";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var changesFromServer = response.Content.ReadFromJsonAsync<SyncQueueServer>().Result;
                    if (changesFromServer != null)
                    {
                        var mappedChanges = EntityMapper.MapSyncQueueServerToSyncQueueClient(changesFromServer);
                        return ApiResult<SyncQueueClient>.Success(mappedChanges);
                    }
                    else
                    {
                        return ApiResult<SyncQueueClient>.Failure("No changes found for the note.", ApiErrorType.NotFound);
                    }

                }
                else
                {
                    return ApiResult<SyncQueueClient>.Failure($"Failed to retrieve changes: {response.ReasonPhrase}", ApiErrorType.ServerError);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error sending notes to server: {ex.Message}");
                return ApiResult<SyncQueueClient>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timeout");
                return ApiResult<SyncQueueClient>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return ApiResult<SyncQueueClient>.Failure($"Error during login: {ex.Message}", ApiErrorType.Unknown);
            }
        }

        internal void UpdateChangtes(SyncQueueClient changesMade)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResult<int>> CreateNewNote(NoteClient currentNote)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseURL}");
                request.Headers.Add("X-User-Id", UserDevice.LocalUser.ToString());  // ← Add header

                var json = JsonConvert.SerializeObject(currentNote);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    // Server returns: { success: true, data: { id: int } }
                    var noteResponse = await response.Content.ReadFromJsonAsync<CreateNoteResponse>();
                    if (noteResponse?.success == true)
                    {
                        return ApiResult<int>.Success(noteResponse.data.id);
                    }
                    return ApiResult<int>.Failure("Server returned unexpected response.", ApiErrorType.ServerError);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
                    return ApiResult<int>.Failure($"Server returned error: {response.StatusCode}", ApiErrorType.ServerError);
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResult<int>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
            }
            catch (TaskCanceledException)
            {
                return ApiResult<int>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
            }
            catch (Exception ex)
            {
                return ApiResult<int>.Failure($"Error creating note: {ex.Message}", ApiErrorType.Unknown);
            }
        }

        private class CreateNoteResponse
        {
            public bool success { get; set; }
            public CreateNoteData data { get; set; }
        }

        private class CreateNoteData
        {
            public int id { get; set; }
        }
    }
}
