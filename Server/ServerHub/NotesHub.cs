using DatabaseLibrary.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Server.ServerHub
{
    public class NotesHub : Hub
    {
        // Thread-safe: ConnectionId -> (UserId, Set<NoteId>)
        private static readonly ConcurrentDictionary<string, (string UserId, HashSet<string> NoteIds)> _connections = new();

        // Thread-safe reverse lookup: UserId -> ConnectionId (latest connection wins)
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connections.TryRemove(Context.ConnectionId, out var entry))
            {
                _userConnections.TryRemove(entry.UserId, out _);
            }
            await base.OnDisconnectedAsync(exception);
        }


        public async Task SubscribeToNote(string idUser, string idNote) // way strings and not GUID
        {
            var groupName = $"note-{idNote}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _connections.AddOrUpdate(
                Context.ConnectionId,
                _ => (idUser, new HashSet<string> { idNote }),
                (_, existing) => { existing.NoteIds.Add(idNote); return existing; }
            );

            // Track latest connection for this user (for GroupExcept exclusion)
            _userConnections[idUser] = Context.ConnectionId;
        }
        // how do we make a new connection
        // way do we send notId when we want to disconnect from the group,
        // should we delete the Note_User connection in the database when we remove connection
        public async Task UnsubscribeFromNote(string noteId)         {
            var groupName = $"note-{noteId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            if (_connections.TryGetValue(Context.ConnectionId, out var entry))
            {
                entry.NoteIds.Remove(noteId);
            }
        }

        public static string? GetConnectionId(string userId)
           => _userConnections.TryGetValue(userId, out var connId) ? connId : null;
    }
}
