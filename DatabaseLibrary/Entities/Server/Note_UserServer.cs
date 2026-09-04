using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace DatabaseLibrary.Entities.Server
{
    [PrimaryKey(nameof(IdUser), nameof(IdNote))]
    [Table("Note_User")]
    public class Note_UserServer : INote_User
    {
        //[Key]
        //[Column(Order = 0)]
        //[ForeignKey("User")]
        public Guid IdUser { get; set; }
        //[Key]
        //[Column(Order = 1)]
        //[ForeignKey("Note")]
        public Guid IdNote { get; set; }
        public UserServer? User { get; set; }
        [JsonIgnore]
        public NoteServer? Note { get; set; }
    }
}
