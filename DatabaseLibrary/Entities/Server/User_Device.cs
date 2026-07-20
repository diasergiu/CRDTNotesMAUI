using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities.Server
{
    [PrimaryKey(nameof(IdUser), nameof(IdDevice))]
    public class User_Device
    {
        public int IdUser { get; set; }
        public int IdDevice { get; set; }

        public UserServer? User { get; set; }
        public Device? Device { get; set; }
    }
}
