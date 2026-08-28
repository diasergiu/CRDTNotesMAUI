using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using MAUIClientUI.MVVM;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;

namespace MAUIClientUI.UserInterface;

public partial class MainPageNotes : ContentPage
{
    private MainPageViewModel _viewModel;
    public MainPageNotes()
    {
        var _noteServices = new DummyNoteServices();
        var _noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
        var dialogHelper = IPlatformApplication.Current.Services.GetService<iDialogHelper>();
        var _authService = IPlatformApplication.Current.Services.GetService<IAuthenticationService>();

        InitializeComponent();
        _viewModel = new MainPageViewModel(_noteServices, _noteRepository, dialogHelper, _authService);

        this.BindingContext = _viewModel;

        
        LoadData();
    }

    private async void LoadData()
    {
        await _viewModel.LoadNotesAsync();

    }
    #region Just navigation to other elements

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var loginPopup = new LoginPopup();
        await Navigation.PushModalAsync(loginPopup);
    }
    #endregion
    protected override async void OnAppearing()
    {
        await _viewModel.LoadNotesAsync();
    }  
}