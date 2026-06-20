using DatabaseLibrary.WrapperClasses;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace DatabaseLibrary.Services
{
    public class RegisterConnectionServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public RegisterConnectionServices(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient() {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<ApiResult<List<NoteInfo>>> RegisterNewUser(String name, string username, string password)
        {
            var requestData = new { Name = name, Username = username, Password = password };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync("/api/register", content);
                if (response.IsSuccessStatusCode)
                {
                    return ApiResult<List<NoteInfo>>.Success(await response.Content.ReadFromJsonAsync<List<NoteInfo>>());
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return ApiResult<List<NoteInfo>>.Failure(errorMessage, ApiErrorType.ServerError);
                }
            }
            catch (Exception ex)
            {
                return ApiResult<List<NoteInfo>>.Failure(ex.Message, ApiErrorType.ConnectionError);
            }
            //catch (HttpRequestException ex)
            //{
            //    ShowStatus($"Connection error: {ex.Message}", true);
            //}
            //catch (TaskCanceledException)
            //{
            //    ShowStatus("Request timeout. Is the server running?", true);
            //}
            //catch (Exception ex)
            //{
            //    ShowStatus($"Error: {ex.Message}", true);
            //}
            //finally
            //{
            //    RegisterButton.IsEnabled = true;
            //    RegisterButton.Text = "Register";
            //}
        } 
    }
}
