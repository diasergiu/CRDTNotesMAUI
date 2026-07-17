using DatabaseLibrary.Entities;
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

        public async Task<ApiResult<List<Note>>> SendAndReceiveNoteUpdates(List<Note> flaggedNotes, string username, string password)
        {
            try
            {
                // Construct relative URL with query parameters
                string url = _baseURL + "/login";

                // Serialize notes to JSON and create content
                var requestObject = new LoginRequest(username, password, flaggedNotes);
                
                var json = JsonConvert.SerializeObject(requestObject);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
              
                // Send POST request with JSON body
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    // Parse the LoginResponse from server
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginRespons>();
                    if (loginResponse?.success == true && loginResponse.notes != null)
                    {
                        return ApiResult<List<Note>>.Success(loginResponse.notes);
                    }
                    else
                    {
                        return ApiResult<List<Note>>.Failure(
                            loginResponse?.message ?? "Login failed",
                            ApiErrorType.ServerError
                        );
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
                    return ApiResult<List<Note>>.Failure($"Server returned error: {response.StatusCode}", ApiErrorType.ServerError);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error sending notes to server: {ex.Message}");
                return ApiResult<List<Note>>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timeout");
                return ApiResult<List<Note>>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notes to server: {ex.Message}");
                return ApiResult<List<Note>>.Failure($"Error sending notes to server: {ex.Message}", ApiErrorType.Unknown);
            }
        }
    }
}
