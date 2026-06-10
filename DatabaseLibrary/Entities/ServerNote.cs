using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class ServerNote : Note
    {
        //[ForeignKey("User")]
        //public int IdOwner { get; set; }
        public List<ServerNoteUser> ServerNoteUser { get; set; }
    }
}
