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
        public Guid IdDevice { get; set; }

        public List<User_Device>? UserDevices { get; set; }
    }
}
