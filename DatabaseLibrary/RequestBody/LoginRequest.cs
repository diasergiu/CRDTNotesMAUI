using DatabaseLibrary.Entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DatabaseLibrary.RequestBody
{
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public List<Note> OfflineNotes { get; set; } = new();

        public LoginRequest(string username, string password, List<Note> offlineNotes)
        {
            Username = username;
            Password = password;
            OfflineNotes = offlineNotes;
        }
    }
}
