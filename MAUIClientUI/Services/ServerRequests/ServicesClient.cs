using DatabaseLibrary.WrapperClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services.ServerRequests
{
    public abstract class ServicesClient
    {
        protected string _baseURL { get; set; }
        protected HttpClient _httpClient;
        protected IUserContext _userContext;

        protected ServicesClient(string URLModifier, IUserContext? userContext = null)
        {
            _baseURL = BaseURLGetter.getBaseURL() + URLModifier;
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(_baseURL),
                Timeout = TimeSpan.FromSeconds(30)
            };
            // Use provided context or fall back to static UserDevice for backward compatibility
            _userContext = userContext ?? new DefaultUserContextAdapter();
        }

        protected HttpRequestMessage GetRequestWithIdHeader(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-User-Id", _userContext.LocalUser.ToString());
            if (!string.IsNullOrEmpty(_userContext.HubConnectionId))
            {
                request.Headers.Add("X-Connection-Id", _userContext.HubConnectionId);
            }
            return request;
        }

        protected async Task<HttpResponseMessage> SendRequest<T>(HttpMethod method, String url, T? data)
        {
            var request = GetRequestWithIdHeader(method, url);
            if (data != null)
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var json = JsonConvert.SerializeObject(data, settings);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            var result = await _httpClient.SendAsync(request);
            return result;
        }
    }

    /// <summary>
    /// Adapter to use static UserDevice as the default IUserContext for backward compatibility.
    /// </summary>
    internal class DefaultUserContextAdapter : IUserContext
    {
        public Guid LocalUser 
        { 
            get => UserDevice.LocalUser; 
            set => UserDevice.LocalUser = value; 
        }

        public string? HubConnectionId 
        { 
            get => UserDevice.HubConnectionId; 
            set => UserDevice.HubConnectionId = value; 
        }
    }
}
