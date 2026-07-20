using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DatabaseLibrary.RequestBody
{
    public class LoginRequest
    {
        //public string Username { get; set; }
        //public string Password { get; set; }
        public UserClient user { get;set; }
        public int IdDevice { get; set; }
        public List<SyncQueueClient> ChangesMade { get; set; } = new();

        public LoginRequest(UserClient user, int deviceId, List<SyncQueueClient> changesMade)
        {
            this.user = user;
            IdDevice = deviceId;
            ChangesMade = changesMade;
        }
    }
}
