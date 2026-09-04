using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Miscellaneous;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;
using MAUIClientUI.Services.ServerRequests;
using MAUIClientUI.UserInterface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.MVVM
{
    public partial class NotesViewModel : ObservableObject
    {
        #region Properties
        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private bool warningVisible;

        private readonly NoteClient _currentNote;
        private bool _isNewNote;
        #endregion

        #region Services/Repositories
        private readonly NoteRepository _noteRepository;
        private readonly NotificationServices _notificationService;
        private readonly INoteServices _noteServices;
        private readonly ILogger<NoteView> _logger;
        private readonly NoteOrchestrator _noteController;
        private readonly iDialogHelper _dialogHelper;
        private readonly INavigationHelper _navigationHelper;
        #endregion

        #region Events
        /// <summary>
        /// Raised when the note content must be pushed into the (CRDT) editor.
        /// The View subscribes and marshals the update onto the main thread,
        /// because the collaborative content cannot be data-bound directly.
        /// </summary>
        public event Action<string> ContentRefreshRequested;
        #endregion

        /// <summary>
        /// The CRDT controller owning the note model. Exposed so the View's platform-specific
        /// input handler can forward keystrokes without touching the CRDT model itself.
        /// </summary>
        public NoteOrchestrator NoteOrchestrator => _noteController;

        public NotesViewModel(NoteClient note, INoteServices noteService, bool isNewNote = false)
        {
            _currentNote = note;
            _isNewNote = isNewNote;
            _noteServices = noteService;

            _noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
            _notificationService = IPlatformApplication.Current.Services.GetService<NotificationServices>();
            _dialogHelper = IPlatformApplication.Current.Services.GetService<iDialogHelper>();
            _navigationHelper = IPlatformApplication.Current.Services.GetService<INavigationHelper>();

            var loggerFactory = IPlatformApplication.Current.Services.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<NoteView>();
            var cursorLogger = loggerFactory?.CreateLogger<CRDTLibrary.Cursor.Document>();
            var characterRepository = IPlatformApplication.Current.Services.GetService<CRDTCharacterRepository>();

            _noteController = new NoteOrchestrator(_currentNote, _noteRepository, _noteServices, characterRepository, cursorLogger);

            //LoadNoteData();
        }

        private void LoadNoteData()
        {
            if (_currentNote != null && !_isNewNote)
            {
                Title = _currentNote.Title;
                ContentRefreshRequested?.Invoke(_noteController.GetText());
            }
        }

        partial void OnTitleChanged(string value)
        {
            WarningVisible = string.IsNullOrWhiteSpace(value);
            _ = PerformSaveAsync(silent: true);
        }

        #region Lifecycle
        [RelayCommand]
        private async Task Appearing()
        {
            LoadNoteData();

            _currentNote.Version = 1;
            _logger?.LogInformation($"NoteView appearing for note: {_currentNote.IdNote}");

            if (UserDevice.LocalUser == Guid.Empty)
            {
                _logger?.LogWarning("Not connecting to real-time notifications: user not logged in.");
                return;
            }

            if (_currentNote != null && !_isNewNote)
            {
                try
                {
                    await _notificationService.SubscribeToNoteAsync(UserDevice.LocalUser, _currentNote.IdNote);
                    _logger?.LogDebug($"Subscribed to note updates for: {_currentNote.IdNote}");

                    _notificationService.NoteUpdated += OnRemoteNoteUpdated;
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error subscribing to notifications: {ex.Message}");
                    await _dialogHelper.ShowAlertAsync("Connection Warning",
                        $"Could not connect to real-time notifications: {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        private async Task Disappearing()
        {
            _logger?.LogInformation($"NoteView disappearing for note: {_currentNote.IdNote}");

            if (_currentNote != null && !_isNewNote)
            {
                try
                {
                    await _notificationService.UnsubscribeFromNoteAsync(_currentNote.IdNote);
                    _logger?.LogDebug($"Unsubscribed from note updates for: {_currentNote.IdNote}");
                    _notificationService.NoteUpdated -= OnRemoteNoteUpdated;
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error unsubscribing: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles updates from other users editing the same note.
        /// </summary>
        private async void OnRemoteNoteUpdated(object sender, CRDTChangePayload e)
        {
            // Filter + persist + merge is handled by the controller; skip UI update if not our note.
            if (!await _noteController.ApplyRemoteChangesAsync(e))
                return;

            ContentRefreshRequested?.Invoke(_noteController.GetText());
        }
        #endregion

        #region Commands
        [RelayCommand]
        private async Task Save() => await PerformSaveAsync(silent: false);

        [RelayCommand]
        private async Task WarningTapped()
        {
            await _dialogHelper.ShowAlertAsync("Warning", "Title is required to save the note.", "OK");
        }

        [RelayCommand]
        private async Task GiveAccess()
        {
            await _navigationHelper.PushAsync(new NoteConnectionPopup(_noteServices, _currentNote.IdNote));
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await _navigationHelper.PopAsync();
        }

        [RelayCommand]
        private async Task DeleteClicked()
        {
            bool confirm = await _dialogHelper.ShowConfirmationAsync("Confirm Delete", "Are you sure you want to delete this note?", "Delete", "Cancel");
            if (!confirm)
            {
                return;
            }
            try
            {
                //bool isOffline = !Connectivity.Current.NetworkAccess.HasFlag(NetworkAccess.Internet);
                if (typeof(DummyNoteServices).IsInstanceOfType(_noteServices))
                {
                    SoftDelete();
                }
                else
                {
                    await HardDeleteNoteAsync();
                }

               
                
                await _navigationHelper.PopAsync();
            }
            catch (Exception ex)
            {
                await _dialogHelper.ShowAlertAsync("Error", $"An error occurred while deleting the note: {ex.Message}", "OK");
            }
        }

        private async Task SoftDelete()
        {
            _noteRepository.SoftDeleteNote(_currentNote);
            await _dialogHelper.ShowAlertAsync("Success", "Note marked for deletion. It will be synced when online.", "OK");
        }

        private async Task HardDeleteNoteAsync()
        {
            var deleteResult = await _noteServices.DeleteNote(_currentNote.IdNote);
            if (!deleteResult.IsSuccess)
            {
                SoftDelete();
                await _dialogHelper.ShowAlertAsync("Error", deleteResult.ErrorMessage, "OK");
                return;
            }
            _noteRepository.DeleteNote(_currentNote);
            await _dialogHelper.ShowAlertAsync("Success", "Note deleted successfully!", "OK");
        }
        #endregion

        #region Save / Conflict resolution
        private async Task PerformSaveAsync(bool silent)
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                if (!silent)
                {
                    WarningVisible = true;
                    await _dialogHelper.ShowAlertAsync("Validation Error", "Please enter a title for the note.", "OK");
                }
                return;
            }

            if (_currentNote == null) return;

            _currentNote.Title = Title;
            _currentNote.DirtyFlagChangesMade = true;

            if (_isNewNote)
            {
                _currentNote.Version = 1;
                _isNewNote = false; // this should fire only if we are logged in

                var createResult = await _noteServices.CreateNewNote(_currentNote);
                if (!createResult.IsSuccess)
                {
                    await _dialogHelper.ShowAlertAsync("Error", createResult.ErrorMessage, "OK");
                    return;
                }
            }
            else
            {
                // Update existing note - CHECK FOR CONFLICTS
                var updateResult = await _noteServices.UpdateNote(_currentNote);

                if (updateResult.IsSuccess)
                {
                    _currentNote.Version = updateResult.Data.ServerNote.Version;
                }
                //// CONFLICT DETECTED - Version mismatch
                //else if (updateResult.Data?.IsVersionConflict == true)
                //{
                //    await ShowConflictDialog(updateResult.Data.ServerNote);
                //    return;  // DO NOT save locally, exit here
                //}

                //// Other errors (not conflict)
                //if (!updateResult.IsSuccess)
                //{
                //    if (!silent)
                //        await _dialogHelper.ShowAlertAsync("Error", updateResult.ErrorMessage, "OK");
                //    return;  // DO NOT save locally on error
                //}

                // TRUE SUCCESS - Only save to local DB when server update succeeds
                _noteRepository.UpdateNote(_currentNote);
            }

            if (!silent)
                await _dialogHelper.ShowAlertAsync("Success", "Note saved successfully!", "OK");
        }

        //    /// <summary>
        //    /// Shows conflict resolution dialog when server update fails due to version mismatch.
        //    /// Does NOT save to local DB - user must choose an action first.
        //    /// </summary>
        //    private async Task ShowConflictDialog(NoteServer serverNote)
        //    {
        //        var action = await _dialogHelper.ShowActionSheetAsync(
        //            "Note Conflict",
        //            "Cancel",
        //            null,
        //            "Use Server Version",
        //            "View Differences",
        //            "Manual Merge");

        //        if (action == "Cancel")
        //        {
        //            // User cancels - do nothing, stay in edit view. Note is NOT saved anywhere.
        //            return;
        //        }
        //        else if (action == "Use Server Version")
        //        {
        //            ApplyServerVersion(serverNote);
        //            await _dialogHelper.ShowAlertAsync("Success", "Updated to server Version. Note saved locally.", "OK");
        //            await _navigationHelper.PopAsync();
        //        }
        //        else if (action == "View Differences")
        //        {
        //            await ShowDetailedComparison(serverNote);
        //        }
        //        else if (action == "Manual Merge")
        //        {
        //            await _dialogHelper.ShowAlertAsync(
        //                "Manual Merge",
        //                $"Server has:\n" +
        //                $"Title: {serverNote.Title}\n\n" +
        //           //     $"Content:\n{serverNote.Content}\n\n" +
        //                $"You can manually edit your Version and try saving again.",
        //                "OK");
        //            // Stay in editor, update version so retry works
        //            _currentNote.Version = serverNote.Version;
        //        }
        //    }

        //    /// <summary>
        //    /// Shows detailed side-by-side comparison of server version vs client version.
        //    /// </summary>
        //    private async Task ShowDetailedComparison(NoteServer serverNote)
        //    {
        //        var action = await _dialogHelper.ShowActionSheetAsync(
        //            "Version Comparison",
        //            "Back",
        //            null,
        //            "Use Server Version",
        //            "Keep My Changes");

        //        if (action == "Use Server Version")
        //        {
        //            ApplyServerVersion(serverNote);
        //            await _dialogHelper.ShowAlertAsync("Success", "Updated to server Version. Note saved locally.", "OK");
        //            await _navigationHelper.PopAsync();
        //        }
        //        else if (action == "Keep My Changes")
        //        {
        //            // Let user keep editing and retry with server version number
        //            _currentNote.Version = serverNote.Version;
        //            await _dialogHelper.ShowAlertAsync("Info", "Updated to match server Version. Try saving again.", "OK");
        //        }
        //    }

        //    private void ApplyServerVersion(NoteServer serverNote)
        //    {
        //        _currentNote.Title = serverNote.Title;
        //        _currentNote.Content = serverNote.Content;
        //        _currentNote.LastUpdate = serverNote.LastUpdate;
        //        _currentNote.Version = serverNote.Version;

        //        // Reflect server version in the UI
        //        Title = _currentNote.Title;
        //        ContentRefreshRequested?.Invoke(_currentNote.Content);

        //        _noteRepository.UpdateNote(_currentNote);
        //    }
        #endregion
    }
}
