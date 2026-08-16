using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatabaseLibrary.Entities
{
    [PrimaryKey(nameof(IdCharacter), nameof(IdNote), nameof(ClientId))]
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
            this.ClientId = characterClient.ClientId;
            this.Operation = characterClient.Operation;
            this.ClockDateTime = characterClient.ClockDateTime;
        }
        public CRDTCharacter(CRDTCharacter characterClient)
        {
            this.IdCharacter = characterClient.IdCharacter;
            this.Character = characterClient.Character;
            this.IdNote = characterClient.IdNote;
            this.Tombstone = characterClient.Tombstone;
            this.ClientId = characterClient.ClientId;
            this.Operation = characterClient.Operation;
            this.ClockDateTime = characterClient.ClockDateTime;
        }
        public CRDTCharacter(char character, decimal IdCharacter)
        {
            this.IdCharacter = IdCharacter;
            this.Character = character;
        }
        public decimal IdCharacter { get; set; }

        public Guid IdNote { get; set; }
        public char Character { get; set; }
        public string Operation { get; set; }
        public string ClockDateTime { get; set; }
        public bool Tombstone { get; set; }
        public Guid ClientId { get; set; } // Essential for conflict resolution

        public CRDTId crdtId()
        {
            return new CRDTId { Position = this.IdCharacter, ClientId = this.ClientId };
        }

    }

    public class CRDTId : IComparable<CRDTId>
    {
        public decimal Position { get; set; }
        public Guid ClientId { get; set; }

        public int CompareTo(CRDTId other)
        {
            int posComparison = this.Position.CompareTo(other.Position);
            if (posComparison != 0)
                return posComparison;

            // Tiebreaker: clientId comparison (lexicographic for deterministic ordering)
            return this.ClientId.CompareTo(other.ClientId);
        }
    }
}
