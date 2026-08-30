using DatabaseLibrary.Entities.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.WrapperClasses
{
    public class DToSendChanges
    {
        public NoteServer NoteServer { get; set; }
        public string Payload { get; set; }
    }
}
