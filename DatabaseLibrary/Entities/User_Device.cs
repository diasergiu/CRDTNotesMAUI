using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities
{
    [PrimaryKey(nameof(IdUser), nameof(IdDevice))]
    public class User_Device
    {
        public int IdUser { get; set; }
        public int IdDevice { get; set; }

        public User User { get; set; }
        public Device Device { get; set; }
    }
}
