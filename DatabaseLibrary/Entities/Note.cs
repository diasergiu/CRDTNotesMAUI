using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class Note 
    {
        //[ForeignKey("User")]
        //public int IdOwner { get; set; }
        [Key]
        public int IdNote { get; set; }
        public string Title { get; set; }
        public string PasswordNote { get; set; }
        public string Content { get; set; }
        public string StartingDate { get; set; }
        public string LastUpdate { get; set; }
        public bool hasPassword { get; set; }
        public List<Note_User> NoteUser { get; set; }
    }
}
