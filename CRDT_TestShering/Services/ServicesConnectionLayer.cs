using CRDT_TestShering.Services.WrapperClasses;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace CRDT_TestShering.Services
{
    public class ServicesConnectionLayer
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        public ServicesConnectionLayer()
        {
            // Configure HttpClient for local server
#if DEBUG
            _baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5000"  // Android emulator
                : "http://localhost:5000"; // Windows/iOS/Mac
#else
            _baseUrl = "https://your-production-api.com";
#endif

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }


        public async Task<ApiResult<LoginResponse>> LoginAstnc(string username, string password)
        {
            try
            {
                var loginRequest = new
                {
                    username = username,
                    password = password
                };

                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    if (loginResponse?.Success == true)
                    {
                        return ApiResult<LoginResponse>.Success(loginResponse);
                    }
                    else
                    {
                        return ApiResult<LoginResponse>.Failure(
                            loginResponse?.Message ?? "Login failed",
                            ApiErrorType.Unauthorized);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return ApiResult<LoginResponse>.Failure(
                        "Invalid username or password",
                        ApiErrorType.Unauthorized
                    );
                }
                else
                {
                    return ApiResult<LoginResponse>.Failure(
                        $"Server error: {response.StatusCode}",
                        ApiErrorType.ServerError
                    );
                }

            }
            catch (HttpRequestException ex)
            {
                return ApiResult<LoginResponse>.Failure(
                    $"Connection error: {ex.Message}. Is the server running?",
                    ApiErrorType.ConnectionError
                );
            }
            catch (TaskCanceledException)
            {
                return ApiResult<LoginResponse>.Failure(
                    "Request timeout. The server is not responding.",
                    ApiErrorType.Timeout
                );
            }
            catch (Exception ex)
            {
                return ApiResult<LoginResponse>.Failure(
                    $"Unexpected error: {ex.Message}",
                    ApiErrorType.Unknown
                );
            }
        }

        /// <summary>
        /// Test server connection
        /// </summary>
        public async Task<ApiResult<bool>> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/test?username=ConnectionTest");

                if (response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Success(true);
                }
                else
                {
                    return ApiResult<bool>.Failure(
                        "Server is reachable but returned an error",
                        ApiErrorType.ServerError
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                return ApiResult<bool>.Failure(
                    $"Cannot reach server: {ex.Message}",
                    ApiErrorType.ConnectionError
                );
            }
            catch (TaskCanceledException)
            {
                return ApiResult<bool>.Failure(
                    "Connection timeout",
                    ApiErrorType.Timeout
                );
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure(
                    $"Error: {ex.Message}",
                    ApiErrorType.Unknown
                );
            }
        }
    }
}


