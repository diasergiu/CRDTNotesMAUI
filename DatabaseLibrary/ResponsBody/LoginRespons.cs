using DatabaseLibrary.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.ResponsBody
{
    public class LoginRespons
    {
        public string? message { get; set; }
        public bool? success { get; set; }
        public List<Note> notes { get; set; }
        
    }
}
