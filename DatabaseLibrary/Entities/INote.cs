using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public interface INote
    {
        public Guid IdNote { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string CreationDate { get; set; }
        public string LastUpdate { get; set; }
        public bool DirtyFlagChangesMade { get; set; }
    }
}
