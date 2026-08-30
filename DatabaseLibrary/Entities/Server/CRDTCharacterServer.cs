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
    [PrimaryKey(nameof(Version), nameof(IdNote))]
    public class CRDTCharacterServer
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Version { get; set; }
        public string Payload { get; set; }
        public Guid IdNote { get; set; }
        [ForeignKey("IdNote")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public NoteServer NoteServer { get; set; }
        public CRDTCharacterServer()
        {
        }
        public CRDTCharacterServer(NoteServer noteServer) : this()
        {
            this.NoteServer = noteServer;
        }
    }
}
