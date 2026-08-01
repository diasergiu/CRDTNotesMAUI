using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities.Client
{
    [Table("SyncQueue")]
    // at this moment will make without changing the title
    public class SyncQueueClient: ISyncQueue
    {
        [Key]
        public int IdSync { get; set; }
        [ForeignKey("Note")]
        public Guid IdNote { get; set; }
        //[ForeignKey("User")]
        public Guid IdUser { get; set; }
        public string Operation { get; set; }
        public string ContentChanges { get; set; }
        public string LastUpdate { get; set; }

        //public UserClient User { get; set; }
        public NoteClient Note { get; set; }
    }
}
