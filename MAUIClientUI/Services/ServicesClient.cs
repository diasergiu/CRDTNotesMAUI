using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Services.HelperClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services
{
    public abstract class ServicesClient
    {
        protected string _baseURL { get; set; }
        protected HttpClient _httpClient;

        protected ServicesClient(string URLModifier)
        {
            _baseURL = BaseURLGetter.getBaseURL() + URLModifier;
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(_baseURL),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }


        protected HttpRequestMessage GetRequestWithIdHeader(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-User-Id", UserDevice.LocalUser.ToString());
            if (!string.IsNullOrEmpty(UserDevice.HubConnectionId))
            {
                request.Headers.Add("X-Connection-Id", UserDevice.HubConnectionId);
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
}
