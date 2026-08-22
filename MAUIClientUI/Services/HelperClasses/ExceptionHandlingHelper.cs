using DatabaseLibrary.WrapperClasses;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace MAUIClientUI.Services.HelperClasses
{
    public static class ExceptionHandlingHelper
    {
        /// <summary>
        /// Executes an async operation and wraps it with consistent exception handling
        /// </summary>
        public static async Task<ApiResult> ExecuteAsync(Func<Task<HttpResponseMessage>> operation, string operationName)
        {
            try
            {
                var response = await operation();

                if (response.IsSuccessStatusCode)
                {
                    return ApiResult.Success();
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
                    return ApiResult.Failure($"Server returned error: {response.StatusCode}", ApiErrorType.ServerError);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error in {operationName}: {ex.Message}");
                return ApiResult.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timeout");
                return ApiResult.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {operationName}: {ex.Message}");
                return ApiResult.Failure($"Error in {operationName}: {ex.Message}", ApiErrorType.Unknown);
            }
        }

        /// <summary>
        /// Generic version for operations that return data
        /// </summary>
        public static async Task<ApiResultData<T>> ExecuteAsync<T>(Func<Task<HttpResponseMessage>> operation, string operationName)
        {
            try
            {
                var response = await operation();

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<T>();
                    return ApiResultData<T>.Success(data);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
                    return ApiResultData<T>.Failure($"Server returned error: {response.StatusCode}", ApiErrorType.ServerError);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error in {operationName}: {ex.Message}");
                return ApiResultData<T>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timeout");
                return ApiResultData<T>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {operationName}: {ex.Message}");
                return ApiResultData<T>.Failure($"Error in {operationName}: {ex.Message}", ApiErrorType.Unknown);
            }
        }

        /// <summary>
        /// Generic version for operations with wrapped responses (data nested in response wrapper).
        /// Automatically extracts the "data" field from the response and populates ApiResultData.
        /// </summary>
        public static async Task<ApiResultData<T>> ExecuteAsyncWithDataExtraction<T>(Func<Task<HttpResponseMessage>> operation, string operationName)
        {
            try
            {
                var response = await operation();

                if (response.IsSuccessStatusCode)
                {
                    // Read the response as a JSON object to extract the data field
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jObject = JObject.Parse(responseContent);

                    // Extract the "data" field and deserialize it to type T
                    var dataToken = jObject["data"];
                    if (dataToken != null)
                    {
                        var data = dataToken.ToObject<T>();
                        return ApiResultData<T>.Success(data);
                    }
                    else
                    {
                        return ApiResultData<T>.Failure("Response does not contain 'data' field", ApiErrorType.ServerError);
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server returned error: {response.StatusCode} - {errorContent}");
                    return ApiResultData<T>.Failure($"Server returned error: {response.StatusCode}", ApiErrorType.ServerError);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error in {operationName}: {ex.Message}");
                return ApiResultData<T>.Failure($"Connection error: {ex.Message}", ApiErrorType.ConnectionError);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timeout");
                return ApiResultData<T>.Failure("Request timeout. The server is not responding.", ApiErrorType.Timeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {operationName}: {ex.Message}");
                return ApiResultData<T>.Failure($"Error in {operationName}: {ex.Message}", ApiErrorType.Unknown);
            }
        }
    }
}
