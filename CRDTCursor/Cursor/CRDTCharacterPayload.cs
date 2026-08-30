using ProtoBuf;

namespace CRDTLibrary.Cursor
{
    // protobuf serialization of CRDT character data.
    // Contains only the essential fields needed for CRDT operations, excluding
    // navigation properties, EF metadata, and client-side state.
    [ProtoContract]
    public class CRDTCharacterPayload
    {

        [ProtoMember(1)]
        public string IdCharacter { get; set; }


        [ProtoMember(2)]
        public char Character { get; set; }


        [ProtoMember(3)]
        public bool Tombstone { get; set; }
    }
}

