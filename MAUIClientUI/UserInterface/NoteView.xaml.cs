using DatabaseLibrary.Cursor;
using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Miscellaneous;
#if ANDROID
using MAUIClientUI.Platforms.Android;
#elif WINDOWS
using MAUIClientUI.Platforms.Windows;
#endif
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MAUIClientUI.UserInterface;

public partial class NoteView : ContentPage
{
    private NoteClient _currentNote;
    private readonly NoteRepository _noteRepository;
    private readonly NotificationServices _notificationService;
    private readonly INoteServices _noteServices;
    private readonly ILogger<NoteView> _logger;
    private bool _isNewNote;
    private NoteController _noteController;
    private IContentEditorInputHandler _inputHandler;


    public NoteView(INoteServices noteService)
    : this(new NoteClient()
    {
        Title = "",
        Content = "",
        CreationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        Version = 1
    }, noteService, true)
    {
        _isNewNote = true;
        _noteRepository.CreateNote(_currentNote);
    }
    public NoteView(NoteClient note, INoteServices noteService, bool isNewNote = false)
    {
        InitializeComponent();
        _currentNote = note;
        _isNewNote = isNewNote;
        _notificationService = IPlatformApplication.Current.Services.GetService<NotificationServices>();
        _noteServices = noteService;
        _noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();

        // Get logger factory and create logger
        var loggerFactory = IPlatformApplication.Current.Services.GetService<ILoggerFactory>();
        _logger = loggerFactory?.CreateLogger<NoteView>();

        var cursorLogger = loggerFactory?.CreateLogger<NoteCursor>();
        _noteController = new NoteController(_currentNote, _noteRepository,
            _noteServices, cursorLogger);

        _logger?.LogInformation($"NoteView initialized for note: {_currentNote.IdNote}");
        LoadNoteData();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _currentNote.Version = 1;
        _logger?.LogInformation($"NoteView appearing for note: {_currentNote.IdNote}");

        //// Subscribe to real-time updates when page appears
        if (_currentNote != null)
        {
            try
            {
                await _notificationService.SubscribeToNoteAsync(UserDevice.LocalUser, _currentNote.IdNote);
                _logger?.LogDebug($"Subscribed to note updates for: {_currentNote.IdNote}");

                // Listen for updates from other users
                _notificationService.NoteUpdated += OnRemoteNoteUpdated;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error subscribing to notifications: {ex.Message}");
                await DisplayAlert("Connection Warning", $"Could not connect to real-time notifications: {ex.Message}", "OK");
            }
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _logger?.LogInformation($"NoteView disappearing for note: {_currentNote.IdNote}");

        // Unsubscribe when leaving the page
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
                System.Diagnostics.Debug.WriteLine($"Error unsubscribing: {ex.Message}");
            }
        }
    }


    /// <summary>
    /// Handles updates from other users editing the same note
    /// </summary>
    /// <summary>
    /// Handles updates from other users editing the same note
    /// </summary>

    private async void OnRemoteNoteUpdated(object sender, CRDTCharacter e)
    {
        // Filter + persist + merge is handled by the controller; skip UI update if not our note
        if (!await _noteController.ApplyRemoteChangeAsync(e))
            return;

        // Marshal the UI update to the main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ContentEditor.Text = _noteController.GetText();
        });
    }

    private void LoadNoteData()
    {
        if (_currentNote != null && !_isNewNote)
        {
            TitleEntry.Text = _currentNote.Title;
            ContentEditor.Text = _noteController.GetText();
        }
    }

    private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        WarningIcon.IsVisible = string.IsNullOrWhiteSpace(e.NewTextValue);
        PerformSaveAsync(silent: true);
    }

    //private void OnContentTextChanged(object sender, TextChangedEventArgs e)
    //{
    //    PerformSaveAsync(silent: true);
    //}


    private async void OnWarningIconTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Warning", "Title is required to save the note.", "OK");
    }

    private async void OnNoteGiveAccess(object sender, EventArgs e)
    {
       await Navigation.PushAsync(new NoteConnectionPopup(_noteServices, _currentNote.IdNote));
    }

    private async Task PerformSaveAsync(bool silent)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            if (!silent)
            {
                WarningIcon.IsVisible = true;
                await DisplayAlert("Validation Error", "Please enter a title for the note.", "OK");
            }
            return;
        }

        if (_currentNote == null) return;

        _currentNote.Title = TitleEntry.Text;
        _currentNote.DirtyFlagChangesMade = true;

        if (_isNewNote)
        {
            _currentNote.Version = 1;
            _isNewNote = false; // this should fire only it we are logged in

            var createResult = await _noteServices.CreateNewNote(_currentNote);
            if (!createResult.IsSuccess)
            {
                await DisplayAlert("Error", createResult.ErrorMessage, "OK");
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
            // CONFLICT DETECTED - Version mismatch
            else if (updateResult.Data?.IsVersionConflict == true)
            {
                await ShowConflictDialog(updateResult.Data.ServerNote);
                return;  // ← DO NOT save locally, exit here
            }

            // Other errors (not conflict)
            if (!updateResult.IsSuccess)
            {
                if (!silent)
                    await DisplayAlert("Error", updateResult.ErrorMessage, "OK");
                return;  // ← DO NOT save locally on error
            }

            // TRUE SUCCESS - Only save to local DB when server update succeeds
            _noteRepository.UpdateNote(_currentNote);
        }

        if (!silent)
            await DisplayAlert("Success", "Note saved successfully!", "OK");
    }

    private void OnEditorHandlerChanged(object sender, EventArgs e)
    {

        if (sender is Editor editor && editor.Handler is not null)
        {
#if WINDOWS
        _inputHandler = new WindowsContentEditorHandler(ContentEditor);
        _inputHandler.CharacterInserted += _noteController.InsertCharacter;
        _inputHandler.CharacterDeleted += _noteController.DeleteCharacter;
        var platformView = (Microsoft.UI.Xaml.Controls.TextBox)editor.Handler.PlatformView;
        if (platformView != null)
        {
            platformView.KeyDown += _inputHandler.HandleKeyPress;
            platformView.KeyUp += _inputHandler.HandleKeyUp;
           // platformView.TextChanging += ContentEditor_TextChanging;
        }
#elif ANDROID
            _inputHandler = new AndroidContentEditorHandler(ContentEditor);
            _inputHandler.CharacterInserted += _noteController.InsertCharacter;
            _inputHandler.CharacterDeleted += _noteController.DeleteCharacter;
        var platformView = (Android.Widget.EditText)editor.Handler.PlatformView;
        if (platformView != null)
        {
            platformView.KeyPress += _inputHandler.HandleKeyPress;
        }
#endif

        }
    }
    /// <summary>
    /// Shows conflict resolution dialog when server update fails due to version mismatch.
    /// Displays server's version alongside client's version for comparison.
    /// Critically: Does NOT save to local DB - user must choose an action first.
    /// </summary>
    private async Task ShowConflictDialog(NoteServer serverNote)
    {
        // Build detailed conflict message
        string conflictMessage =
            $"⚠️ CONFLICT DETECTED\n\n" +
            $"This note was modified by another user.\n\n" +
            $"Server Version (Current):\n" +
            $"  Title: {serverNote.Title}\n" +
            $"  Updated: {serverNote.LastUpdate}\n" +
            $"  Version: {serverNote.Version}\n\n" +
            $"Your Version (Unsaved):\n" +
            $"  Title: {TitleEntry.Text}\n" +
            $"  Version: {_currentNote.Version}";

        // Show conflict dialog with three options
        var action = await DisplayActionSheet(
            "Note Conflict",
            "Cancel",
            null,
            "Use Server Version",
            "View Differences",
            "Manual Merge"
        );

        if (action == "Cancel")
        {
            // User cancels - do nothing, stay in edit view
            // Note is NOT saved anywhere
            return;
        }
        else if (action == "Use Server Version")
        {
            // Replace user's changes with server version
            // Update local object with server version
            _currentNote.Title = serverNote.Title;
            _currentNote.Content = serverNote.Content;
            _currentNote.LastUpdate = serverNote.LastUpdate;
            _currentNote.Version = serverNote.Version;

            // Update UI to show server version
            TitleEntry.Text = _currentNote.Title;
            ContentEditor.Text = _currentNote.Content;

            // Save to local DB with server's version
            _noteRepository.UpdateNote(_currentNote);

            await DisplayAlert("Success", "Updated to server Version. Note saved locally.", "OK");
            await Navigation.PopAsync();
        }
        else if (action == "View Differences")
        {
            // Show detailed side-by-side comparison
            await ShowDetailedComparison(serverNote);
        }
        else if (action == "Manual Merge")
        {
            // Allow user to manually edit and keep trying
            // User can now see server version and manually merge
            await DisplayAlert(
                "Manual Merge",
                $"Server has:\n" +
                $"Title: {serverNote.Title}\n\n" +
                $"Content:\n{serverNote.Content}\n\n" +
                $"You can manually edit your Version and try saving again.",
                "OK"
            );
            // Stay in editor, user can now edit manually
            _currentNote.Version = serverNote.Version;  // Update version so retry works
        }
    }

    /// <summary>
    /// Shows detailed side-by-side comparison of server version vs client version.
    /// </summary>
    private async Task ShowDetailedComparison(NoteServer serverNote)
    {
        string comparison =
            $"╔════════════════════════════════╗\n" +
            $"║     CONFLICT COMPARISON         ║\n" +
            $"╚════════════════════════════════╝\n\n" +

            $"📌 SERVER VERSION (Current on Server):\n" +
            $"────────────────────────────────────\n" +
            $"Title:       {serverNote.Title}\n" +
            $"Last Update: {serverNote.LastUpdate}\n" +
            $"Version:     {serverNote.Version}\n" +
            $"Content Preview:\n" +
            $"{(serverNote.Content?.Length > 100 ? serverNote.Content.Substring(0, 100) + "..." : serverNote.Content)}\n\n" +

            $"📝 YOUR VERSION (Not Yet Saved):\n" +
            $"────────────────────────────────────\n" +
            $"Title:       {TitleEntry.Text}\n" +
            $"Last Update: {_currentNote.LastUpdate}\n" +
            $"Version:     {_currentNote.Version}\n" +
            $"Content Preview:\n" +
            $"{(ContentEditor.Text?.Length > 100 ? ContentEditor.Text.Substring(0, 100) + "..." : ContentEditor.Text)}\n";

        // Show comparison dialog
        var action = await DisplayActionSheet(
            "Version Comparison",
            "Back",
            null,
            "Use Server Version",
            "Keep My Changes"
        );

        if (action == "Use Server Version")
        {
            // Apply server version
            _currentNote.Title = serverNote.Title;
            _currentNote.Content = serverNote.Content;
            _currentNote.LastUpdate = serverNote.LastUpdate;
            _currentNote.Version = serverNote.Version;

            TitleEntry.Text = _currentNote.Title;
            ContentEditor.Text = _currentNote.Content;

            _noteRepository.UpdateNote(_currentNote);
            await DisplayAlert("Success", "Updated to server Version. Note saved locally.", "OK");
            await Navigation.PopAsync();
        }
        else if (action == "Keep My Changes")
        {
            // Let user keep editing and retry with server version number
            _currentNote.Version = serverNote.Version;
            await DisplayAlert("Info", "Updated to match server Version. Try saving again.", "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete this note?", "Delete", "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            var deleteResult = await _noteServices.DeleteNote(_currentNote.IdNote);
            if (!deleteResult.IsSuccess)
            {
                await DisplayAlert("Error", deleteResult.ErrorMessage, "OK");
                return;
            }

            _noteRepository.DeleteNote(_currentNote);
            _noteRepository.DeleteCharacterByNoteId(_currentNote.IdNote);
            await DisplayAlert("Success", "Note deleted successfully!", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while deleting the note: {ex.Message}", "OK");
        }
    }
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}