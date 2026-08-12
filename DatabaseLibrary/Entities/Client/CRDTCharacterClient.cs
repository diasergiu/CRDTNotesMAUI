using DatabaseLibrary.Entities.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace DatabaseLibrary.Entities.Client
{
    public class CRDTCharacterClient: CRDTCharacter
    {
        [ForeignKey("IdNote")]
        [JsonIgnore]
        public NoteClient NoteClient { get; set; }
    }
}
