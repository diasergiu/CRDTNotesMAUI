using DatabaseLibrary.Entities;
using DatabaseLibrary.WrapperClasses;
using System.Net.Http.Json;
using System.Text;

namespace MAUIClientUI.Services
{
    public class RegisterConnectionServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly LoginConnectionServices _loginConnectionServices;

        public RegisterConnectionServices(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient() {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _loginConnectionServices = new LoginConnectionServices(baseUrl);
        }

        public async Task<ApiResult<List<Note>>> RegisterNewUser(String name, string username, string password, List<Note> listOfNotes)
        {
            var requestData = new { Name = name, Username = username, Password = password };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync("/api/register", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await _loginConnectionServices.LoginAsync(username, password);
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
