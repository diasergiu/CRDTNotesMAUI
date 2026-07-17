using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities
{
    [Table("user")]
    public class UserServer : IUser
    {
        [Key]
        public int IdUser { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<Note_UserServer>? NotesUsers { get; set; }
        public List<User_Device>? UserDevices { get; set; }
    }
}
