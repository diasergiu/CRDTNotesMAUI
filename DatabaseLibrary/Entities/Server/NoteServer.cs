using DatabaseLibrary.Entities.Client;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace DatabaseLibrary.Entities.Server
{
    [Table("Notes")]
    public class NoteServer : INote
    {
        [Key]
        public Guid IdNote { get; set; }
        public string Title { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool DirtyFlagChangesMade { get; set; }
        public int Version { get; set; }
        public List<Note_UserServer>? NoteUser { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public List<CRDTCharacterServer>? CRDTCharacter { get; set; }
    }
}
