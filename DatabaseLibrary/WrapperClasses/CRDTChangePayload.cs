namespace DatabaseLibrary.WrapperClasses;

/// <summary>
/// Transport DTO for sending CRDT changes to the server.
/// Contains the note ID and an encoded (encrypted, serialized) payload of CRDT character changes.
/// </summary>
public class CRDTChangePayload
{
    /// <summary>
    /// The ID of the note being modified.
    /// </summary>
    public Guid IdNote { get; set; }

    /// <summary>
    /// The Base64-encoded, encrypted protobuf payload containing the list of CRDT character changes.
    /// </summary>
    public string Payload { get; set; }

    public CRDTChangePayload() { }

    public CRDTChangePayload(Guid idNote, string payload)
    {
        IdNote = idNote;
        Payload = payload;
    }
}
