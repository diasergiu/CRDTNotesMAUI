using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class UserClient
    {
        [Key]
        public int IdUser { get; set; }
        public string Name { get; set; }
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        public List<Note_User> NotesUsers { get; set; }
    }
}
