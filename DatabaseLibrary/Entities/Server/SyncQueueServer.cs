using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities.Server
{
    [Table("SyncQueue")]
    public class SyncQueueServer : ISyncQueue
    {
        [Key]
        public int IdSync { get; set; }
        [ForeignKey("Note")]
        public int IdNote { get; set; }
        [ForeignKey("Device")]
        public int IdDevice { get; set; }
        [ForeignKey("User")]
        public int IdUser { get; set; }
        public string Operation { get; set; }
        public string ContentChanges { get; set; }
        public string LastUpdate { get; set; }

        public UserServer User { get; set; }
        public NoteServer Note { get; set; }
        public Device Device { get; set; }
    }
}
