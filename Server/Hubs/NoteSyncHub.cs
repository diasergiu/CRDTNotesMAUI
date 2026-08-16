using DatabaseLibrary.Entities;
using Microsoft.AspNetCore.SignalR;
using Server.ServerHub;
using System.Threading.Channels;


public class NoteSyncHub
{
    private IHubContext<NotesHub> _notesHubContext;

    public NoteSyncHub(IHubContext<NotesHub> notesHubContext)
    {
        _notesHubContext = notesHubContext;
    }

    public async Task PushUpdatesToSubscribedUserAsync(List<CRDTCharacter> changes, Guid currentUser)
    {

        foreach (CRDTCharacter c in changes)
        {
            if (_notesHubContext != null)
            {
                var groupName = $"note-{c.IdNote}";
                var senderConnectionId = NotesHub.GetConnectionId(currentUser.ToString());

                var sendTask = senderConnectionId != null
                    ? _notesHubContext.Clients.GroupExcept(groupName, senderConnectionId)
                    : _notesHubContext.Clients.Group(groupName);

                try
                {
                    await sendTask.SendAsync("NoteUpdated", c);
                    // Success - log or track it
                    Console.WriteLine($"Message sent successfully for note {c.IdNote}");
                }
                catch (Exception ex)
                {
                    // Failure - see what went wrong
                    Console.WriteLine($"Failed to send message for note {c.IdNote}: {ex.Message}");
                }
            }
        }

    }
}
