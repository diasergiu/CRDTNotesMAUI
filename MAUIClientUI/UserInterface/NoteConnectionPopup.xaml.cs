using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;

namespace MAUIClientUI.UserInterface;

public partial class NoteConnectionPopup : ContentPage
{

	private NoteRepository _noteRepository;
	private INoteServices _noteServices;
	public NoteConnectionPopup(INoteServices noteService)
	{
		_noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>(); ;
		_noteServices = noteService;
		InitializeComponent();
		
	}
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

	private async void OnAccessNoteClicked(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(IdNoteEntry.Text))
		{
			await DisplayAlert("Error", "Please enter a Note ID", "OK");
			return;
		}

		if (!Guid.TryParse(IdNoteEntry.Text, out var guid))
		{
			await DisplayAlert("Error", "Invalid Note ID format. Please enter a valid GUID.", "OK");
			return;
		}

		try
		{
			var result = await _noteServices.GetNote(guid);
			if (result.IsSuccess)
			{
				_noteRepository.createNote(result.Data);
				var noteData = await _noteServices.GetAllCharacterByNote(guid);
				_noteRepository.saveCRDTChanges(noteData.Data.Select(a => new CRDTCharacterClient(a)).ToList());
				await Navigation.PopModalAsync();
			}
			else
			{
				await DisplayAlert("Error", "Failed to access note. Please check the ID and try again.", "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}
}