using DatabaseLibrary.Entities.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services.ServerRequests
{
    public class CRDTCharacterServices : ServicesClient
    {
        public CRDTCharacterServices(string URLModifier) : base(URLModifier)
        {
        }

        public List<CRDTCharacterClient> GetAllCRDTCharactersFromNote(Guid IdNote)
        {
            var response = _httpClient.GetAsync($"{_baseURL}/GetAllCRDTCharactersFromNote/{IdNote}").Result;
            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadAsStringAsync().Result;
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<CRDTCharacterClient>>(result);
            }
            else
            {
                throw new Exception($"Error retrieving CRDT characters from note: {response.ReasonPhrase}");
            }
        }
    }
}
