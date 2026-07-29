using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.ResponsBody
{
    public class LoginRespons
    {
        public string? message { get; set; }
        public bool? success { get; set; }
        public int IdUser { get; set; }
        public List<ISyncQueue> ChangesToMake { get; set; }

    }
}
