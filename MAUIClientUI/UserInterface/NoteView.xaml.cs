using DatabaseLibrary.Entities;
using MAUIClientUI.MVVM;

namespace MAUIClientUI.UserInterface;

public partial class NoteView : ContentPage
{
	private Note _currentNote;
	private bool _isNewNote;

	public NoteView(Note note, bool isNewNote = false)
	{
		InitializeComponent();
		_currentNote = note;
		_isNewNote = isNewNote;
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

		if (_currentNote != null)
		{
			_currentNote.Title = TitleEntry.Text;
			_currentNote.Content = ContentEditor.Text ?? "";
			_currentNote.CreationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _currentNote.LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");			
			_currentNote.HasPassword = IsSecured.IsChecked;
            _currentNote.PasswordNote = IsSecured.IsChecked ? PasswordEntry.Text : "";
			_currentNote.DirtyFlagChangesMade = true;

            using (var dbContext = new DbContextClient())
			{
				if (_isNewNote)
				{
					dbContext.Notes.Add(_currentNote);
				}
				else
				{
					dbContext.Notes.Update(_currentNote);
				}

				await dbContext.SaveChangesAsync();
			}

			await DisplayAlert("Success", "Note saved successfully!", "OK");
			await Navigation.PopAsync();
		}
	}

	private async void OnCancelClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}