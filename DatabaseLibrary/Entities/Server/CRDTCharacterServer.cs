using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities.Server
{
    public class CRDTCharacterServer: CRDTCharacter
    {
        [ForeignKey("IdNote")]
        public NoteServer NoteServer { get; set; }
    }
}
