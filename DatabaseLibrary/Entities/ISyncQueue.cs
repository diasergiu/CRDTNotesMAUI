using Microsoft.EntityFrameworkCore.Migrations.Operations;
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
                Guid IdNote { get; set; }
                Guid IdUser { get; set; }
                string ContentChanges { get; set; }
                string LastUpdate { get; set; }
           }
    
   
    
}
