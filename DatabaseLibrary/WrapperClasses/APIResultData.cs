namespace DatabaseLibrary.WrapperClasses
{
    /// <summary>
    /// Represents the result of an API operation
    /// </summary>
    public class ApiResultData<T> : ApiResult   
    {
        public T Data { get; set; }

        public static ApiResultData<T> Success(T data)
        {
            return new ApiResultData<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public static ApiResultData<T> Failure(string errorMessage, ApiErrorType errorType = ApiErrorType.Unknown)
        {
            return new ApiResultData<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorType = errorType
            };
        }
    }

}