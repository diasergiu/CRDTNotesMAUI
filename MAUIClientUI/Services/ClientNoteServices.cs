using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.RequestBody;
using DatabaseLibrary.ResponsBody;
using DatabaseLibrary.WrapperClasses;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
    }
}
