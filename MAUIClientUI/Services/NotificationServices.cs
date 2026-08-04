using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services
{
    public class NotificationServices
    {
        private HubConnection _hubConnection;
        private bool _isConnected = false;

        public event EventHandler<NoteUpdateEventArgs> NoteUpdated;
        public event EventHandler<string> ConnectionStatusChanged;

        public NotificationServices(string serverUrl)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl}/notesHub")
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10) })
                .Build();
            
            _hubConnection.On<dynamic>("NoteUpdated", (data) =>
            {
                try
                {
                    var args = new NoteUpdateEventArgs
                    {
                        NoteId = data.GetProperty("noteId").GetGuid(),  
                        Title = data.GetProperty("title").GetString(),
                        Content = data.GetProperty("content").GetString(),
                        LastUpdate = DateTime.ParseExact(data.GetProperty("lastUpdate").GetString(),
                        "yyyy-MM-dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture),
                        Version = data.GetProperty("version").GetInt32()
                    };
                    NoteUpdated?.Invoke(this, args);
                }
                catch (KeyNotFoundException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Missing property in NoteUpdated message: {ex.Message}");
                }
            });

            _hubConnection.Reconnected += (connectionId) =>
            {
                ConnectionStatusChanged?.Invoke(this, "Reconnected");
                return Task.CompletedTask;
            };

            _hubConnection.Closed += (ex) =>
            {
                ConnectionStatusChanged?.Invoke(this, "Disconnected");
                return Task.CompletedTask;
            };
        }

        public async Task ConnectAsync()
        {
            if (_isConnected) return;

            try
            {
                await _hubConnection.StartAsync();
                _isConnected = true;
                ConnectionStatusChanged?.Invoke(this, "Connected");
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
                throw ex;
            }
        }

        public async Task SubscribeToNoteAsync(Guid userId, Guid noteId)
        {
            if (!_isConnected) await ConnectAsync();
            
            await _hubConnection.InvokeAsync("SubscribeToNote", userId.ToString(), noteId.ToString());
        }

        public async Task UnsubscribeFromNoteAsync(Guid noteId)
        {
            if (_isConnected)
            {
                await _hubConnection.InvokeAsync("UnsubscribeFromNote", noteId.ToString());
            }
        }

        public async Task DisconnectAsync()
        {
            if (_isConnected)
            {
                await _hubConnection.StopAsync();
                _isConnected = false;
            }
        }

        public bool IsConnected => _isConnected;
    }

    public class NoteUpdateEventArgs : EventArgs
    {
        public Guid NoteId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime LastUpdate { get; set; }
        public int Version { get; set; }
    }

}
