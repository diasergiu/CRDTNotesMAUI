using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.WrapperClasses
{
    public class ApiResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public ApiErrorType ErrorType { get; set; }

        public static ApiResult Success()
        {
            return new ApiResult
            {
                IsSuccess = true,
                ErrorMessage = null,
                ErrorType = ApiErrorType.Unknown
            };
        }

        public static ApiResult Failure(string errorMessage, ApiErrorType errorType = ApiErrorType.Unknown)
        {
            return new ApiResult
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
