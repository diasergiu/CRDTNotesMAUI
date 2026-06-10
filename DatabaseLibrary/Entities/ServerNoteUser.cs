using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class ServerNoteUser
    {
        [Key]
        [Column(Order = 0)]
        [ForeignKey("User")]
        public int IdUser { get; set; }
        [Key]
        [Column(Order = 1)]
        [ForeignKey("ServerNotes")]
        public int IdServerNote { get; set; }

        public User User { get; set; }
        public ServerNote ServerNotes { get; set; }
    }
}
