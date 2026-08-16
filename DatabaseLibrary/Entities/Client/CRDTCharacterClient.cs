using DatabaseLibrary.Entities.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities.Client
{
    public class CRDTCharacterClient: CRDTCharacter
    {
        [ForeignKey("IdNote")]
        [JsonIgnore]
        public NoteClient NoteClient { get; set; }

    }
}
