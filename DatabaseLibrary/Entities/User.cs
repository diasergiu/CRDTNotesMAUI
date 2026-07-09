using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseLibrary.Entities
{

    public class User : UserClient
    {
        
        public List<User_Device> UserDevices { get; set; }
    }
}
