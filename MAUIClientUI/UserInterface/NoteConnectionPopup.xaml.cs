using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;

namespace MAUIClientUI.UserInterface;

public partial class NoteConnectionPopup : ContentPage
{

	private NoteRepository _noteRepository;
	private INoteServices _noteServices;
	private Guid _noteId;
	public NoteConnectionPopup(INoteServices noteService, Guid IdNote)
	{
		_noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>(); ;
		_noteServices = noteService;
		_noteId = IdNote;
		InitializeComponent();
		
	}
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

	private async void OnAccessNoteClicked(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(IdNoteEntry.Text))
		{
			await DisplayAlert("Error", "Please enter a Note ID", "OK");
			return;
		}

		if (!Guid.TryParse(IdNoteEntry.Text, out var userId))
		{
			await DisplayAlert("Error", "Invalid User ID format. Please enter a valid GUID.", "OK");
			return;
		}

		try
		{
			var result = await _noteServices.GiveNoteAccessToUser(_noteId, userId);
			if (result.IsSuccess)
			{
				await Navigation.PopModalAsync();
			}
			else
			{
				await DisplayAlert("Error", "Failed to find User. Please check the ID and try again.", "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
}