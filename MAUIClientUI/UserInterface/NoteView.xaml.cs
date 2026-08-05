using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.MVVM;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using Microsoft.EntityFrameworkCore;

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

    public NoteView(NoteClient note, INoteServices noteService, bool isNewNote = false)
    {
        InitializeComponent();
		_currentNote = note;
		_isNewNote = isNewNote;
		_databaseService = IPlatformApplication.Current.Services.GetService<IDatabaseServices>();
		_noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
        _notificationService = IPlatformApplication.Current.Services.GetService<NotificationServices>();
        _noteServices = noteService;
            //new NoteServices("/api/notes"); // should split clientNoteServices into multiple classes
        LoadNoteData();
	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to real-time updates when page appears
        if (_currentNote != null && !_isNewNote)
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
    private async void OnRemoteNoteUpdated(object sender, NoteUpdateEventArgs e)
    {
        // Filter: only handle updates for the note currently being viewed
        if (e.NoteId != _currentNote?.IdNote) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            bool hasLocalChanges = TitleEntry.Text != _currentNote.Title
                                || ContentEditor.Text != _currentNote.Content;

            bool accept = true;
            if (hasLocalChanges)
            {
                accept = await DisplayAlert(
                    "Note Updated",
                    "Another user changed this note. You have unsaved changes. Overwrite your changes with the latest version?",
                    "Accept", "Keep Mine");
            }

            if (accept)
            {
                _currentNote.Title = e.Title;
                _currentNote.Content = e.Content;
                _currentNote.LastUpdate = e.LastUpdate.ToString("yyyy-MM-dd HH:mm:ss");
                _currentNote.Version = e.Version;

                TitleEntry.Text = e.Title;
                ContentEditor.Text = e.Content;
            }
        });
    }

    private void LoadNoteData()
	{
		if (_currentNote != null && !_isNewNote)
		{
			TitleEntry.Text = _currentNote.Title;
			ContentEditor.Text = _currentNote.Content;
		}
	}

	private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
	{
		WarningIcon.IsVisible = string.IsNullOrWhiteSpace(e.NewTextValue);
		TriggerAutoSave();
	}

	private void OnContentTextChanged(object sender, TextChangedEventArgs e)
	{
		TriggerAutoSave();
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
		await PerformSaveAsync(silent: false);
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
		_currentNote.CreationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		_currentNote.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		_currentNote.DirtyFlagChangesMade = true;

		if (_isNewNote)
		{
			_currentNote.Version = 1;
			_noteRepository.createNote(_currentNote);

			var createResult = await _noteServices.CreateNewNote(_currentNote);
			if (!createResult.IsSuccess)
			{
  				await DisplayAlert("Error", createResult.ErrorMessage, "OK");
				return;
			}
			_isNewNote = false;
		}
		else
		{
			// Update existing note - CHECK FOR CONFLICTS
			var updateResult = await _noteServices.UpdateNote(_currentNote);

			if (updateResult.IsSuccess)
			{
				_currentNote.Version = updateResult.ServerNote.Version;
			}
			// CONFLICT DETECTED - Version mismatch
			else if (updateResult.IsVersionConflict)
			{
				await ShowConflictDialog(updateResult.ServerNote);
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

	private async void OnCancelClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
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
}