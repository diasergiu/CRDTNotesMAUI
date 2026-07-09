using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities
{
    [PrimaryKey(nameof(IdUser), nameof(IdNote))]
    public class Note_User
    {
        //[Key]
        //[Column(Order = 0)]
        //[ForeignKey("User")]
        public int IdUser { get; set; }
        //[Key]
        //[Column(Order = 1)]
        //[ForeignKey("Note")]
        public int IdNote { get; set; }

        public UserClient User { get; set; }
        public Note Note { get; set; }
    }
}
