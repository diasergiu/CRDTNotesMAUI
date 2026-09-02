using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Newtonsoft.Json;
using DatabaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLibrary.Entities.Client
{
    [Table("Note")]
    public class NoteClient : INote
    {
        //[ForeignKey("User")]
        //public int IdOwner { get; set; }
        [Key]
        public Guid IdNote { get; set; }
        public string Title { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdate { get; set; }
        public int Version { get; set; }
        public bool DirtyFlagChangesMade { get; set; }
        public bool isDeleted { get; set; }
        [JsonIgnore]
        public List<CRDTCharacterClient>? CRDTCharacter { get; set; }

    }
}
