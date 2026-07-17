using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public interface INote_User
    {
        public int IdNote { get; set; }
        public int IdUser { get; set; }
    }
}
