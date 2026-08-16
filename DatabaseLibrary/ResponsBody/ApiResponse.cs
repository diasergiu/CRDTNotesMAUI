using System.Text.Json.Serialization;

namespace DatabaseLibrary.ResponsBody
{
    /// <summary>
    /// Standard API response envelope for all endpoints.
    /// Ensures consistency between client and server.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the response</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates whether the request succeeded.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// The response data (null on error).
        /// </summary>
        [JsonPropertyName("data")]
        public T Data { get; set; }

        /// <summary>
        /// Error or informational message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Error code for categorization (optional).
        /// </summary>
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        /// <summary>
        /// Create a successful response with data.
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// Create an error response.
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, string errorCode = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Data = default,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// For endpoints that don't return data (e.g., DELETE operations).
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
    }
}
