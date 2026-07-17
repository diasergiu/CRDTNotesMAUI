using DatabaseLibrary.Entities;
using DatabaseLibrary.WrapperClasses;
using System;
using System.Net.Http.Json;
using System.Text;

namespace MAUIClientUI.Services
{

    public class LoginConnectionServices : ServicesClient
    {
        private readonly ClientNoteServices _noteServices;

        public LoginConnectionServices(string baseUrl) : base(baseUrl) { }

        public async Task<ApiResult<List<Note>>> LoginAsync(string username, string password)
        {
            try
            {
                string url = $"{_baseURL}/login?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
                    return ApiResult<List<Note>>.Success(notes);

                }
                else
                {
                    string errorMessage = await response.Content.ReadAsStringAsync();
                    return ApiResult<List<Note>>.Failure(
                        $"Server returned error: {response.StatusCode}. Message: {errorMessage}",
                        ApiErrorType.ServerError
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResult<List<Note>>.Failure(
                    $"Connection error: {ex.Message}. Is the server running?",
                    ApiErrorType.ConnectionError
                );
            }
            catch (TaskCanceledException)
            {
                return ApiResult<List<Note>>.Failure(
                    "Request timeout. The server is not responding.",
                    ApiErrorType.Timeout
                );
            }
            catch (Exception ex)
            {
                return ApiResult<List<Note>>.Failure(
                    $"Unexpected error: {ex.Message}",
                    ApiErrorType.Unknown
                );
            }

        }

        public async Task<ApiResult<List<Note>>> RegisterNewUser(String name, string username, string password, List<Note> listOfNotes)
        {
            var requestData = new { Name = name, Username = username, Password = password };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync("/api/user/register", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await LoginAsync(username, password);
                    return ApiResult<List<Note>>.Success(result.Data);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return ApiResult<List<Note>>.Failure(errorMessage, ApiErrorType.ServerError);
                }
            }
            catch (Exception ex)
            {
                return ApiResult<List<Note>>.Failure(ex.Message, ApiErrorType.ConnectionError);
            }
        }

    }

}