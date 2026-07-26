using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
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
	private readonly ClientNoteServices _clientNoteServices;
	private bool _isNewNote;
	private int _IdUser;

	public NoteView(NoteClient note, bool isNewNote = false)
	{
		InitializeComponent();
		_currentNote = note;
		_isNewNote = isNewNote;
		_databaseService = IPlatformApplication.Current.Services.GetService<IDatabaseServices>();
		_noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
		_clientNoteServices = new ClientNoteServices("/api/notes/"); // should split clientNoteServices into multiple classes
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
        SyncQueueClient changesMade = new SyncQueueClient();
        if (_currentNote != null)
		{
			_currentNote.Title = TitleEntry.Text;
			_currentNote.Content = ContentEditor.Text ?? "";
			_currentNote.CreationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _currentNote.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");			
			_currentNote.HasPassword = IsSecured.IsChecked;
            _currentNote.PasswordNote = IsSecured.IsChecked ? PasswordEntry.Text : "";
			_currentNote.DirtyFlagChangesMade = true;

            // save Changes to SyncQueueClient
            changesMade.Note = _currentNote;
			changesMade.ContentChanges = ContentEditor.Text ?? "";
			changesMade.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			changesMade.IdUser = UserDevice.LocalUser;
            
			if (_isNewNote)
            {
                //_clientNoteServices.SaveChangesIfOnline(changesMade, DeviceIdentityService.GetDeviceId());
				var createResult = await _clientNoteServices.CreateNewNote(_currentNote);
				if (createResult.IsSuccess)
				{
					_currentNote.IdNote = createResult.Data;
				}
				else
				{
					await DisplayAlert("Error", createResult.ErrorMessage, "OK");
					return;
				}
            }
            else
            {
                _clientNoteServices.UpdateChangtes(changesMade);
            }
            _noteRepository.SaveChangesNotes(_currentNote, changesMade, _isNewNote);
			


            await DisplayAlert("Success", "Note saved successfully!", "OK");
			await Navigation.PopAsync();
		}
	}

	private async void OnCancelClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}