using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Newtonsoft.Json;

namespace DatabaseLibrary.Entities.Server
{
    public class CRDTCharacterServer: CRDTCharacter
    {
        [ForeignKey("IdNote")]
        [JsonIgnore]
        public NoteServer NoteServer { get; set; }
        public CRDTCharacterServer()
        {
        }

        public CRDTCharacterServer(CRDTCharacter character)
        {
            this.IdCharacter = character.IdCharacter;
            this.IdNote = character.IdNote;
            this.Character = character.Character;
            this.Operation = character.Operation;
            this.ClockDateTime = character.ClockDateTime;
            this.Tombstone = character.Tombstone;
            this.ClientId = character.ClientId;
            this.IsDirtyFlag = character.IsDirtyFlag;
        }

        public CRDTCharacterServer(CRDTCharacter character, NoteServer noteServer) : this(character)
        {
            this.NoteServer = noteServer;
        }
    }
}
