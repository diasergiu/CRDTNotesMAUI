using DatabaseLibrary.Entities;
using DatabaseLibrary.WrapperClasses;
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

        public event EventHandler<CRDTCharacter> NoteUpdated;
        public event EventHandler<string> ConnectionStatusChanged;

        public NotificationServices(string serverUrl)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl}/notesHub", options =>
                {
                    options.Headers.Add("X-User-Id", UserDevice.LocalUser.ToString());
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10) })
                .Build();
            
            _hubConnection.On<CRDTCharacter>("NoteUpdated", (data) =>
            {
                try
                {
                    
                    NoteUpdated?.Invoke(this, data);
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
        public CRDTCharacter crdtCharacter { get; set; }
        //public decimal IdCharacter { get; set; }
        //public Guid IdNote { get; set; }
        //public char Character { get; set; }
        //public string Operation { get; set; }
        //public string ClockDateTime { get; set; }
        //public bool Tombstone { get; set; }
        //public Guid ClientId { get; set; } // Essential for conflict resolution
        //public bool IsDirtyFlag { get; set; }
    }

}
