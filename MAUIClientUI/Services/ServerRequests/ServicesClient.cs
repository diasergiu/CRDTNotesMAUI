using DatabaseLibrary.WrapperClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
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
            // Prefer explicit context (tests), then DI singleton (MAUI app), then fresh instance (other hosts)
            _userContext = userContext
                ?? IPlatformApplication.Current?.Services.GetService<IUserContext>()
                ?? new UserContext();
        }

        protected HttpRequestMessage GetRequestWithIdHeader(HttpMethod method, string url, Guid? noteId = null)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-User-Id", _userContext.LocalUser.ToString());
            if (!string.IsNullOrEmpty(_userContext.HubConnectionId))
            {
                request.Headers.Add("X-Connection-Id", _userContext.HubConnectionId);
            }
            if(noteId != null)
            {
                request.Headers.Add("noteId", noteId.ToString());
            }
            return request;
        }

        protected async Task<HttpResponseMessage> SendRequest<T>(HttpMethod method, string url, T? data, Guid? noteId = null)
        {
            var request = noteId.HasValue
                ? GetRequestWithIdHeader(method, url, noteId.Value)
                : GetRequestWithIdHeader(method, url);

            SerializeJson<T>(request, data);
            var result = await _httpClient.SendAsync(request);
            return result;
        }
        protected void SerializeJson<T>(HttpRequestMessage request, T? data)
        {
            if (data != null)
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var json = JsonConvert.SerializeObject(data, settings);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
        }        
    }

    }
