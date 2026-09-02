using DatabaseLibrary.Entities.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.WrapperClasses
{
    public class DTOSendChanges
    {
        public NoteServer NoteServer { get; set; }
        public string Payload { get; set; }
    }
}
