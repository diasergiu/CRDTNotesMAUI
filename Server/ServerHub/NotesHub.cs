using DatabaseLibrary.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Server.ServerHub
{
    public class NotesHub : Hub
    {
        // Thread-safe: ConnectionId -> (UserId, Set<NoteId>)
        private static readonly ConcurrentDictionary<string, (string UserId, HashSet<string> NoteIds)> _connections = new();

        // Thread-safe reverse lookup: UserId -> all active ConnectionIds of that user
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userConnections = new();
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connections.TryRemove(Context.ConnectionId, out var entry))
            {
                if (_userConnections.TryGetValue(entry.UserId, out var connectionIds))
                {
                    connectionIds.TryRemove(Context.ConnectionId, out _);
                    if (connectionIds.IsEmpty)
                    {
                        _userConnections.TryRemove(entry.UserId, out _);
                    }
                }
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

            // Track every connection of this user
            _userConnections.GetOrAdd(idUser, _ => new ConcurrentDictionary<string, byte>())[Context.ConnectionId] = 0;
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

        public static IReadOnlyCollection<string> GetConnectionIds(string userId)
           => _userConnections.TryGetValue(userId, out var connIds)
                ? connIds.Keys.ToArray()
                : Array.Empty<string>();

        public static bool IsConnectionOfUser(string userId, string connectionId)
           => _userConnections.TryGetValue(userId, out var connIds) && connIds.ContainsKey(connectionId);
    }
}
