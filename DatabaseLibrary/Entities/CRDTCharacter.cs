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


        public CRDTCharacter()
        {
            
        }

        public CRDTCharacter(int left, int right, char character, int IdCharacter)
        {
            this.IdCharacter = IdCharacter;
            IdLeftCharacter = left;
            IdRightCharacter = right;
            this.Character = character;
        }
        public  decimal IdCharacter { get; set; }
        public decimal? IdLeftCharacter { get; set; }
        public decimal? IdRightCharacter { get; set; }
        public Guid IdNote { get; set; }
        public char Character { get; set; }
        public string Opperation { get; set; } 
        public string ClockDateTime { get; set; }
        public bool Tombstone { get; set; }
        public Guid ClientId { get; set; } // Essential for conflict resolution
        //[ForeignKey("IdLeftCharacter")]
        //public CRDTCharacter LeftCharacter { get; set; }
        //[ForeignKey("IdRightCharacter")]
        //public CRDTCharacter RightCharacter { get; set; }
    }
}
