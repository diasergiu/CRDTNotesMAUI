using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.RequestBody;
using DatabaseLibrary.ResponsBody;
using DatabaseLibrary.WrapperClasses;
using SlackAPI;
using System;
using System.Net.Http.Json;
using System.Text;

namespace MAUIClientUI.Services
{

    public class UserServices : ServicesClient
    {
        private readonly ClientServices _noteServices;

        public UserServices(string baseUrl) : base(baseUrl) { }


        // old function that returned a list of ISyncQueue, Synqing data is dose outsite of login now

        //public async Task<ApiResult<List<ISyncQueue>>> LoginAsync(string username, string password)
        //{
        //    try
        //    {
        //        string url = $"{_baseURL}/login?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
        //        var response = await _httpClient.GetAsync(url);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var notes = await response.Content.ReadFromJsonAsync<List<ISyncQueue>>();
        //            return ApiResult<List<ISyncQueue>>.Success(notes);

        //        }
        //        else
        //        {
        //            string errorMessage = await response.Content.ReadAsStringAsync();
        //            return ApiResult<List<ISyncQueue>>.Failure(
        //                $"Server returned error: {response.StatusCode}. Message: {errorMessage}",
        //                ApiErrorType.ServerError
        //            );
        //        }
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        return ApiResult<List<ISyncQueue>>.Failure(
        //            $"Connection error: {ex.Message}. Is the server running?",
        //            ApiErrorType.ConnectionError
        //        );
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        return ApiResult<List<ISyncQueue>>.Failure(
        //            "Request timeout. The server is not responding.",
        //            ApiErrorType.Timeout
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResult<List<ISyncQueue>>.Failure(
        //            $"Unexpected error: {ex.Message}",
        //            ApiErrorType.Unknown
        //        );
        //    }

        //}

        public async Task<ApiResultData<UserClient>> Login(string username, string password)
        {
            try
            {
                string url = $"{_baseURL}/login?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

                var response = _httpClient.GetAsync(url).Result;
                var responseData = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

                if (response.IsSuccessStatusCode && responseData?.Success == true)
                {
                    var userData = (System.Text.Json.JsonElement)responseData.Data;
                    var idUserStr = userData.GetProperty("idUser").GetString();

                    if (Guid.TryParse(idUserStr, out var userId))
                    {
                        var user = new UserClient { IdUser = userId };
                        UserDevice.localUser(user.IdUser);
                        Console.WriteLine(UserDevice.LocalUser);
                        return ApiResultData<UserClient>.Success(user);
                    }
                }

                return ApiResultData<UserClient>.Failure(
                    responseData?.Message ?? $"Login failed: {response.ReasonPhrase}",
                    ApiErrorType.ServerError
                );
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error sending notes to server: {ex.Message}");
                return ApiResultData<UserClient>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timeout");
                return ApiResultData<UserClient>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return ApiResultData<UserClient>.Failure($"Error during login: {ex.Message}", ApiErrorType.Unknown);
            }
        }

        public async Task<ApiResultData<UserClient>> RegisterNewUser(String name, string username, string password)
        {
            var requestData = new { Name = name, Username = username, Password = password };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync("/api/user/register", content);
                var responseData = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

                if (response.IsSuccessStatusCode && responseData?.Success == true)
                {
                    var userData = (System.Text.Json.JsonElement)responseData.Data;
                    var idUserStr = userData.GetProperty("idUser").GetString();
                    var userName = userData.GetProperty("username").GetString();
                    var userNameProp = userData.GetProperty("name").GetString();

                    if (Guid.TryParse(idUserStr, out var userId))
                    {
                        var user = new UserClient 
                        { 
                            IdUser = userId,
                            Username = userName,
                            Name = userNameProp
                        };
                        return ApiResultData<UserClient>.Success(user);
                    }
                }

                return ApiResultData<UserClient>.Failure(
                    responseData?.Message ?? "Registration failed.",
                    ApiErrorType.ServerError
                );
            }
            catch (Exception ex)
            {
                return ApiResultData<UserClient>.Failure(ex.Message, ApiErrorType.ConnectionError);
            }
        }

    }

}