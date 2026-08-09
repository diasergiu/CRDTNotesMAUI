using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatabaseLibrary.Entities
{
    [PrimaryKey(nameof(IdCharacter), nameof(IdNote))]
    public class CRDTCharacter
    {
        public int IdCharacter { get; set; }
        public int? IdLeftCharacter { get; set; }
        public int? IdRightCharacter { get; set; }
        public Guid IdNote { get; set; }
        public char Character { get; set; }
        public string Opperation { get; set; } 
        public string ClockDateTime { get; set; }
        public bool Tombstone { get; set; }
        //[ForeignKey("IdLeftCharacter")]
        //public CRDTCharacter LeftCharacter { get; set; }
        //[ForeignKey("IdRightCharacter")]
        //public CRDTCharacter RightCharacter { get; set; }
    }
}
