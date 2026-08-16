using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Cursor;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using System.Diagnostics;

namespace MAUIClientUI.UserInterface;

public partial class NoteView : ContentPage
{
    private NoteClient _currentNote;
    private readonly IDatabaseServices _databaseService;
    private readonly NoteRepository _noteRepository;
    //private readonly ClientServices _clientNoteServices;
    private readonly NotificationServices _notificationService;
    private readonly INoteServices _noteServices;
    private bool _isNewNote;
    private CancellationTokenSource _autoSaveCts;
    private CRDTCharacterRepository _crdtCharactetrRepository;
    private NoteCursor _noteCursor;
    private int _lastCursorPosition = 0; // Track cursor position
    private string _lastEditorText = ""; // Track previous editor text for character detection


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
        _noteRepository.createNote(_currentNote);
    }
    public NoteView(NoteClient note, INoteServices noteService, bool isNewNote = false)
    {
        InitializeComponent();
        _currentNote = note;
        _isNewNote = isNewNote;
        _databaseService = IPlatformApplication.Current.Services.GetService<IDatabaseServices>();
        _notificationService = IPlatformApplication.Current.Services.GetService<NotificationServices>();
        _crdtCharactetrRepository = IPlatformApplication.Current.Services.GetService<CRDTCharacterRepository>();
        _noteServices = noteService;
        _noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
        _noteCursor = new NoteCursor(_currentNote.CRDTCharacter, DeviceIdentityService.GetCurrentUserId());
        //new NoteServices("/api/notes"); // should split clientNoteServices into multiple classes
        LoadNoteData();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _currentNote.Version = 1;

       // var createResult = await _noteServices.CreateNewNote(_currentNote);
        //if (createResult.IsSuccess)
        //{
        //    _isNewNote = false;
        //}
        //// Subscribe to real-time updates when page appears
        if (_currentNote != null)
        {
            try
            {
                await _notificationService.SubscribeToNoteAsync(UserDevice.LocalUser, _currentNote.IdNote);

                // Listen for updates from other users
                _notificationService.NoteUpdated += OnRemoteNoteUpdated;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Connection Warning", $"Could not connect to real-time notifications: {ex.Message}", "OK");
            }
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe when leaving the page
        if (_currentNote != null && !_isNewNote)
        {
            try
            {
                await _notificationService.UnsubscribeFromNoteAsync(_currentNote.IdNote);
                _notificationService.NoteUpdated -= OnRemoteNoteUpdated;
            }
            catch (Exception ex)
            {
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
        // Filter: only handle updates for the note currently being viewed
        if (e.IdNote != _currentNote?.IdNote) return;

        _noteCursor.MergeCharacter(new CRDTCharacterClient(e));

        // Marshal the UI update to the main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ContentEditor.Text = _noteCursor.GetString();
        });
    }

    private void LoadNoteData()
    {
        if (_currentNote != null && !_isNewNote)
        {
            TitleEntry.Text = _currentNote.Title;
            ContentEditor.Text = _noteCursor.GetString();
            _lastEditorText = ContentEditor.Text;
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

    /// <summary>
    /// Platform-specific: Get cursor position from Editor
    /// </summary>
    private int GetEditorCursorPosition(Editor editor)
    {
        try
        {
#if ANDROID
            var handler = editor.Handler as Microsoft.Maui.Handlers.EditorHandler;
            if (handler?.PlatformView is Android.Widget.EditText editText)
                return editText.SelectionStart;
#elif IOS
        var handler = editor.Handler as Microsoft.Maui.Handlers.EditorHandler;
        if (handler?.PlatformView is UIKit.UITextView textView)
            return (int)textView.SelectedRange.Location;
#elif WINDOWS
        var handler = editor.Handler as Microsoft.Maui.Handlers.EditorHandler;
        if (handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
            return textBox.SelectionStart;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting cursor position: {ex.Message}");
        }

        return 0;
    }

    private void TriggerAutoSave()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts = new CancellationTokenSource();
        var token = _autoSaveCts.Token;

        Task.Delay(800, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                MainThread.BeginInvokeOnMainThread(() => _ = PerformSaveAsync(silent: true));
        }, TaskScheduler.Default);
    }

    private async void OnWarningIconTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Warning", "Title is required to save the note.", "OK");
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
       // await PerformSaveAsync(silent: false);
    }

    private async Task SendChangesToServer(CRDTCharacter change)
    {
        List<CRDTCharacter> characters = new List<CRDTCharacter>();
        characters.Add(change);
        await _noteServices.SendCRDTChangestoServer(characters);
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
        _currentNote.Content = ContentEditor.Text ?? "";
        _currentNote.DirtyFlagChangesMade = true;

        if (_isNewNote)
        {
            _currentNote.Version = 1;

            var createResult = await _noteServices.CreateNewNote(_currentNote);
            if (!createResult.IsSuccess)
            {
                await DisplayAlert("Error", createResult.ErrorMessage, "OK");
                return;
            }
            _isNewNote = false; // this should fire only it we are logged in
           
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
            _noteRepository.updateNote(_currentNote);
        }

        if (!silent)
            await DisplayAlert("Success", "Note saved successfully!", "OK");
    }

    private void OnEditorHandlerChanged(object sender, EventArgs e)
    {

        if (sender is Editor editor && editor.Handler is not null)
        {
#if WINDOWS
        var platformView = (Microsoft.UI.Xaml.Controls.TextBox)editor.Handler.PlatformView;
        if (platformView != null)
        {
            platformView.KeyDown += ContentEditor_KeyDown;
            platformView.KeyUp += ContentEditor_KeyUp;
           // platformView.TextChanging += ContentEditor_TextChanging;
        }
#elif ANDROID
            var platformView = (Android.Widget.EditText)editor.Handler.PlatformView;
            if (platformView != null)
            {
                platformView.KeyPress += ContentEditor_KeyPress;
            }
#endif
        }
    }

#if WINDOWS
    private void ContentEditor_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // Only handle special keys here (backspace, delete, enter, etc.)
        // Regular character input is handled by ContentEditor_TextComposition
        int cursorPosition = GetEditorCursorPosition(ContentEditor);
        var key = e.Key.ToString();
        if (e.Key == Windows.System.VirtualKey.Back)
        {
            var leftCharacter = _noteCursor.deleteCharacterToTheLeft(cursorPosition + 1);
            if (leftCharacter != null)
            {
                _crdtCharactetrRepository.UpdateCharacter(leftCharacter);
            }
            e.Handled = true;
            SendChangesToServer(new CRDTCharacter(leftCharacter));
            Debug.WriteLine("Backspace pressed");
        }
        else if (key.Length == 1 || key == "Space")
        {

            if(key == "Space"){
                key = " ";
            }
           // GetCharacterFromInput(key[0]);
            Debug.WriteLine("Key pressed", e.Key.ToString());
        
            var newCharacter = _noteCursor.InsertCharacter(cursorPosition, e.Key.ToString()[0]);
            newCharacter.IdNote = _currentNote.IdNote; // not the best solution replace it later
            _crdtCharactetrRepository.SaveNewCrdtCharacter(newCharacter);
            SendChangesToServer(new CRDTCharacter(newCharacter));
       
       }
       

    }

private void ContentEditor_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
{

    Debug.WriteLine($"Key Up: {e.Key.ToString()}");
}

private void ContentEditor_TextChanging(Microsoft.UI.Xaml.Controls.TextBox sender, Microsoft.UI.Xaml.Controls.TextBoxTextChangingEventArgs args)
{
    // TextChanging fires before the text is actually changed
    // We need to get the actual character being added
    string oldText = _lastEditorText ?? "";
    string newText = sender.Text ?? "";

    // Only process if exactly one character was added
  //  if (newText.Length == oldText.Length + 1)
  //  {
        // Find which character was added
        int insertPosition = -1;
        for (int i = 0; i < oldText.Length; i++)
        {
            if (i >= newText.Length || oldText[i] != newText[i])
            {
                insertPosition = i;
                break;
            }
        }

        // If no difference found, character was added at the end
        if (insertPosition == -1)
        {
            insertPosition = oldText.Length;
        }

        char typedCharacter = oldText[insertPosition];
        int cursorPosition = sender.SelectionStart;

        GetCharacterFromInput(typedCharacter);

        var newCharacterId = _noteCursor.InsertCharacter(cursorPosition - 1, typedCharacter);
        newCharacterId.IdNote = _currentNote.IdNote;
        _crdtCharactetrRepository.SaveNewCrdtCharacter(newCharacterId);
        Debug.WriteLine($"Character Typed: {typedCharacter}");
  //  }

    _lastEditorText = newText;
}
#elif ANDROID
    private void ContentEditor_KeyPress(object sender, Android.Views.View.KeyEventArgs e)
    {
        if (e.Event.Action == Android.Views.KeyEventActions.Down)
        {
            CRDTCharacter CRDT = GetCharacterFromInput((char)e.Event.UnicodeChar);
            Debug.WriteLine($"Key Pressed: {CRDT.Character}");
        }
    }
#endif

    private CRDTCharacter GetCharacterFromInput(char character)
    {
        CRDTCharacter CRDT = new CRDTCharacter();
        CRDT.Character = character;
        CRDT.ClockDateTime = DateTime.Now.ToString("yyyy-MM-dd");
        return CRDT;
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
            _noteRepository.updateNote(_currentNote);

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

            _noteRepository.updateNote(_currentNote);
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
        if (_isNewNote)
        {
            await DisplayAlert("Cannot Delete", "Cannot delete a new note that hasn't been saved.", "OK");
            return;
        }

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

            _noteRepository.deleteNote(_currentNote);
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