using DatabaseLibrary.Entities.Client;
using MAUIClientUI.MVVM;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;

namespace MAUIClientUI.UserInterface;

public partial class NoteConnectionPopup : ContentPage
{
	private NoteConnectionViewModel _noteConnectionViewModel;
	public NoteConnectionPopup(INoteServices noteService, Guid IdNote)
	{
		var dialogHelper = IPlatformApplication.Current.Services.GetService<iDialogHelper>(); 
		var navigationHelper = IPlatformApplication.Current.Services.GetService<INavigationHelper>();

		_noteConnectionViewModel = new NoteConnectionViewModel( noteService, dialogHelper, navigationHelper, IdNote);

		InitializeComponent();
        BindingContext = _noteConnectionViewModel;
	}
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

	
}