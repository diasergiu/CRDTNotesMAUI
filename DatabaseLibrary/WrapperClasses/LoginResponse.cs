using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.WrapperClasses
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string AuthToken { get; set; }
        public UserInfo User { get; set; }
        public List<NoteInfo> Notes { get; set; }
        public string Message { get; set; }
    }
}
