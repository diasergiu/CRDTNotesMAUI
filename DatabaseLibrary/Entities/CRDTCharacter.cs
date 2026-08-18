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
        public CRDTCharacter(CRDTCharacterClient characterClient)
        {
            this.IdCharacter = characterClient.IdCharacter;
            this.Character = characterClient.Character;
            this.IdNote = characterClient.IdNote;
            this.Tombstone = characterClient.Tombstone;
            this.Operation = characterClient.Operation;
            this.ClockDateTime = characterClient.ClockDateTime;
        }
        public CRDTCharacter(CRDTCharacter characterClient)
        {
            this.IdCharacter = characterClient.IdCharacter;
            this.Character = characterClient.Character;
            this.IdNote = characterClient.IdNote;
            this.Tombstone = characterClient.Tombstone;
            this.Operation = characterClient.Operation;
            this.ClockDateTime = characterClient.ClockDateTime;
        }
        public CRDTCharacter(char character, string IdCharacter)
        {
            this.IdCharacter = IdCharacter;
            this.Character = character;
        }

        /// <summary>
        /// Composite ID string in format: decimal (simple) or (pos,site)(pos,site)... (composite)
        /// Used as primary key component for conflict resolution
        /// </summary>
        public string IdCharacter { get; set; }

        public Guid IdNote { get; set; }
        public char Character { get; set; }
        public string Operation { get; set; }
        public string ClockDateTime { get; set; }
        public bool Tombstone { get; set; }
    }
}
