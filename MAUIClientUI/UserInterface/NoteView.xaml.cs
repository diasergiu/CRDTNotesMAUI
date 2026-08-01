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
	private readonly INoteServices _noteServices;
    private bool _isNewNote;

	public NoteView(NoteClient note, INoteServices noteService, bool isNewNote = false)
	{
		InitializeComponent();
		_currentNote = note;
		_isNewNote = isNewNote;
		_databaseService = IPlatformApplication.Current.Services.GetService<IDatabaseServices>();
		_noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
		_noteServices = noteService;
            //new NoteServices("/api/notes"); // should split clientNoteServices into multiple classes
        LoadNoteData();
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
	}

	private async void OnWarningIconTapped(object sender, EventArgs e)
	{
		await DisplayAlert("Warning", "Title is required to save the note.", "OK");
	}

	private async void OnSaveClicked(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(TitleEntry.Text))
		{
			WarningIcon.IsVisible = true;
			await DisplayAlert("Validation Error", "Please enter a title for the note.", "OK");
			return;
		}
        //SyncQueueClient changesMade = new SyncQueueClient();
		if (_currentNote != null)
		{
			_currentNote.Title = TitleEntry.Text;
			_currentNote.Content = ContentEditor.Text ?? "";
			_currentNote.CreationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			_currentNote.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");			
			_currentNote.DirtyFlagChangesMade = true;			

            // save Changes to SyncQueueClient
   //         changesMade.Note = _currentNote;
			//changesMade.ContentChanges = ContentEditor.Text ?? "";
			//changesMade.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			//changesMade.IdUser = UserDevice.LocalUser;
            
			if (_isNewNote)
            {
                _currentNote.version = 1;
                _noteRepository.createNote(_currentNote);

                //_clientNoteServices.SaveChangesIfOnline(changesMade, DeviceIdentityService.GetDeviceId());
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
					_currentNote.version = updateResult.ServerNote.version;
				}
				// CONFLICT DETECTED - Version mismatch
				else if (updateResult.IsVersionConflict)
				{
					var serverNote = updateResult.ServerNote;

					// Show conflict dialog with both versions
					await ShowConflictDialog(serverNote);
					return;  // ← DO NOT save locally, exit here
				}

				// Other errors (not conflict)
				if (!updateResult.IsSuccess)
				{
					await DisplayAlert("Error", updateResult.ErrorMessage, "OK");
					return;  // ← DO NOT save locally on error
				}

				// TRUE SUCCESS - Only save to local DB when server update succeeds
				_noteRepository.updateNote(_currentNote);
			}

			


            await DisplayAlert("Success", "Note saved successfully!", "OK");
			await Navigation.PopAsync();
		}
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
			$"  Version: {serverNote.version}\n\n" +
			$"Your Version (Unsaved):\n" +
			$"  Title: {TitleEntry.Text}\n" +
			$"  Version: {_currentNote.version}";

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
			_currentNote.version = serverNote.version;

			// Update UI to show server version
			TitleEntry.Text = _currentNote.Title;
			ContentEditor.Text = _currentNote.Content;

			// Save to local DB with server's version
			_noteRepository.updateNote(_currentNote);

			await DisplayAlert("Success", "Updated to server version. Note saved locally.", "OK");
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
				$"You can manually edit your version and try saving again.",
				"OK"
			);
			// Stay in editor, user can now edit manually
			_currentNote.version = serverNote.version;  // Update version so retry works
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
			$"Version:     {serverNote.version}\n" +
			$"Content Preview:\n" +
			$"{(serverNote.Content?.Length > 100 ? serverNote.Content.Substring(0, 100) + "..." : serverNote.Content)}\n\n" +

			$"📝 YOUR VERSION (Not Yet Saved):\n" +
			$"────────────────────────────────────\n" +
			$"Title:       {TitleEntry.Text}\n" +
			$"Last Update: {_currentNote.LastUpdate}\n" +
			$"Version:     {_currentNote.version}\n" +
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
			_currentNote.version = serverNote.version;

			TitleEntry.Text = _currentNote.Title;
			ContentEditor.Text = _currentNote.Content;

			_noteRepository.updateNote(_currentNote);
			await DisplayAlert("Success", "Updated to server version. Note saved locally.", "OK");
			await Navigation.PopAsync();
		}
		else if (action == "Keep My Changes")
		{
			// Let user keep editing and retry with server version number
			_currentNote.version = serverNote.version;
			await DisplayAlert("Info", "Updated to match server version. Try saving again.", "OK");
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