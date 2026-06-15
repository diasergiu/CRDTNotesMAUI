using DatabaseLibrary.WrapperClasses;
using System;
using System.Net.Http.Json;

namespace DatabaseLibrary.Services
{

    public class LoginConnectionServices
    {

        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public LoginConnectionServices(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<ApiResult<List<NoteInfo>>> LoginAsync(string username, string password)
        {
            try
            {
                string url = $"{_baseUrl}?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var notes = await response.Content.ReadFromJsonAsync<List<NoteInfo>>();
                    return ApiResult<List<NoteInfo>>.Success(notes);

                }
                else
                {
                    string errorMessage = await response.Content.ReadAsStringAsync();
                    return ApiResult<List<NoteInfo>>.Failure(
                        $"Server returned error: {response.StatusCode}. Message: {errorMessage}",
                        ApiErrorType.ServerError
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResult<List<NoteInfo>>.Failure(
                    $"Connection error: {ex.Message}. Is the server running?",
                    ApiErrorType.ConnectionError
                );
            }
            catch (TaskCanceledException)
            {
                return ApiResult<List<NoteInfo>>.Failure(
                    "Request timeout. The server is not responding.",
                    ApiErrorType.Timeout
                );
            }
            catch (Exception ex)
            {
                return ApiResult<List<NoteInfo>>.Failure(
                    $"Unexpected error: {ex.Message}",
                    ApiErrorType.Unknown
                );
            }

        }

    }

}
