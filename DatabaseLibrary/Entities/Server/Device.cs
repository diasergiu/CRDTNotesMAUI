using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DatabaseLibrary.Entities.Server
{
    public class Device
    {
        [Key]
        public int IdDevice { get; set; }
        [ForeignKey("User")]
        public int IdUser { get; set; }

        public List<User_Device>? UserDevices { get; set; }
    }
}
