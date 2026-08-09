using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;

namespace MAUIClientUI.UserInterface;

public partial class NoteConnectionPopup : ContentPage
{

	private NoteRepository _noteRepository;
	private NoteServices _noteServices;
	public NoteConnectionPopup()
	{
		InitializeComponent();
		
	}
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

	private async void OnAccessNoteClicked(object sender, EventArgs e)
	{
		//NoteClient note = _noteServices.GetNote(IdNoteEntry.Text); // need to change NoteController to verify if IdNote is in the server and so see if the user has access to the Note. Give access if it dose
		//_noteRepository.createNote(note);
    }
}