using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class Note
    {
        [Key]
        public int IdNote { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string StartingDate { get; set; }
        public string lastUpdate { get; set; }
    }
}
