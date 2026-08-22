using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Services.HelperClasses;

namespace MAUIClientUI.Services
{

    public class UserServices : ServicesClient
    {
        public UserServices(string baseUrl) : base(baseUrl) { }

        public async Task<ApiResultData<UserClient>> Login(string username, string password)
        {
            var result = await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<UserClient>(
                async () => await SendRequest<object>(
                    HttpMethod.Get,
                    $"{_baseURL}/login?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}",
                    null),
                nameof(Login));

            if (result.IsSuccess && result.Data != null)
            {
                UserDevice.localUser(result.Data.IdUser);
            }

            return result;
        }

        public async Task<ApiResultData<UserClient>> RegisterNewUser(string name, string username, string password)
        {
            var requestData = new { Name = name, Username = username, Password = password };

            return await ExceptionHandlingHelper.ExecuteAsyncWithDataExtraction<UserClient>(
                async () => await SendRequest(HttpMethod.Post, $"{_baseURL}/register", requestData),
                nameof(RegisterNewUser));
        }

    }

}