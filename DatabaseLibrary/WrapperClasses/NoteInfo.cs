using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.WrapperClasses
{
    // what is different from Note Class
    //  Ai completes (is that this class is used to send data to the client and it doesn't have the UserId property because it's not needed on the client side
    public class NoteInfo
    {
        public int IdNote { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
