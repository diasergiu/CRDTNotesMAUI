using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities.Client
{
    [PrimaryKey(nameof(IdUser), nameof(IdNote))]
    [Table("Note_User")]
    public class Note_UserClient : INote_User
    {
        //[Key]
        //[Column(Order = 0)]
        //[ForeignKey("User")]
        public Guid IdUser { get; set; }
        //[Key]
        //[Column(Order = 1)]
        //[ForeignKey("Note")]
        public Guid IdNote { get; set; }
        public UserClient? User { get; set; }
        public NoteClient? Note { get; set; }
        public int version { get; set ;}

        public Note_UserClient()
        {
                
        }
    }
}
