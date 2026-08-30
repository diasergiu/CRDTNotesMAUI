using DatabaseLibrary.Entities;
using Microsoft.AspNetCore.SignalR;
using Server.ServerHub;
using System.Threading.Channels;
using DatabaseLibrary.WrapperClasses;


public class NoteSyncHub
{
    private IHubContext<NotesHub> _notesHubContext;

    public NoteSyncHub(IHubContext<NotesHub> notesHubContext)
    {
        _notesHubContext = notesHubContext;
    }

    public async Task PushUpdatesToSubscribedUserAsync(CRDTChangePayload noteUpdate, Guid currentUser, string? senderConnectionId = null)
    {
        // Only exclude the connection the update actually came from. Excluding "the"
        // connection of the user breaks when the same user is signed in from two apps.
        if (senderConnectionId != null && !NotesHub.IsConnectionOfUser(currentUser.ToString(), senderConnectionId))
        {
            senderConnectionId = null;
        }
        if (_notesHubContext != null)
        {
            var groupName = $"note-{noteUpdate.IdNote}";

            var sendTask = senderConnectionId != null
                ? _notesHubContext.Clients.GroupExcept(groupName, senderConnectionId)
                : _notesHubContext.Clients.Group(groupName);

            try
            {
                await sendTask.SendAsync("NoteUpdated", noteUpdate);
                // Success - log or track it
                Console.WriteLine($"Message sent successfully for note {noteUpdate.IdNote}");
            }
            catch (Exception ex)
            {
                // Failure - see what went wrong
                Console.WriteLine($"Failed to send message for note {noteUpdate.IdNote}: {ex.Message}");
            }
        }
    }

}
