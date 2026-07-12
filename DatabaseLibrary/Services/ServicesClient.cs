using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Services
{
    public abstract class ServicesClient
    {
        string baseURL;
        private HttpClient httpClient;

        protected ServicesClient(string URLModifier)
        {
            baseURL = URLModifier;
            httpClient = new HttpClient()
            {
                //BaseAddress = new Uri(BaseURLGetter.getBaseURL() + baseURL)
            };
        }
    }
}
