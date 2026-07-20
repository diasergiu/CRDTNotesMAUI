using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities.Server
{
    [Table("Note")]
    public class NoteServer : INote
    {
        [Key]
        public int IdNote { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string CreationDate { get; set; }
        public string LastUpdate { get; set; }
        public bool HasPassword { get; set; }
        public string PasswordNote { get; set; }
        public bool DirtyFlagChangesMade { get; set; }
        public List<Note_UserServer>? NoteUser { get; set; }
    }
}
