namespace DatabaseLibrary.WrapperClasses
{
    /// <summary>
    /// Represents the result of an API operation
    /// </summary>
    public class ApiResult<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public ApiErrorType ErrorType { get; set; }

        public static ApiResult<T> Success(T data)
        {
            return new ApiResult<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public static ApiResult<T> Failure(string errorMessage, ApiErrorType errorType = ApiErrorType.Unknown)
        {
            return new ApiResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorType = errorType
            };
        }
    }

    public enum ApiErrorType
    {
        Unknown,
        ConnectionError,
        Timeout,
        Unauthorized,
        NotFound,
        ServerError,
        ValidationError
    }
}