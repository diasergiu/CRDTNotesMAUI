using DatabaseLibrary.Entities.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities.Client
{
    [Table("CRDTCharacters")]
    public class CRDTCharacterClient: CRDTCharacter
    {

        public CRDTCharacterClient()
        {
                
        }
        public CRDTCharacterClient(CRDTCharacter e) : base(e)
        {
            IsDirtyFlag = false;
        }
        [ForeignKey("IdNote")]
        [JsonIgnore]
        public NoteClient NoteClient { get; set; }
        public bool IsDirtyFlag { get; set; }

    }
}
