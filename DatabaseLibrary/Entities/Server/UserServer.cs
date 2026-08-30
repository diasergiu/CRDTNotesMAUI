using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities.Server
{
    [Table("User")]
    public class UserServer : IUser
    {
        [Key]
        public Guid IdUser { get; set; }
        public string Name { get; set; }
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        public List<Note_UserServer>? NotesUser { get; set; }
    }
}
