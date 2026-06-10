using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class User
    {
        [Key]
        public int IdUser { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<ServerNoteUser> ServerNotesUsers { get; set; } 
        public List<Device> Devices { get; set; }
    }
}
