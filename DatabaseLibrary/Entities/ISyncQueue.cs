using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities
{
   public interface ISyncQueue
            {
                int IdSync { get; set; }
                int IdNote { get; set; }
                int IdUser { get; set; }
                string Operation { get; set; }
                string ContentChanges { get; set; }
                string LastUpdate { get; set; }
           }
    
    
}
