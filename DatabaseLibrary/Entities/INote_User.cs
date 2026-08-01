using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public interface INote_User
    {
        public Guid IdNote { get; set; }
        public Guid IdUser { get; set; }
    }
}
