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
    }
}
