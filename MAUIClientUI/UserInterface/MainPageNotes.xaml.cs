using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using MAUIClientUI.MVVM;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;
using Microsoft.Extensions.Logging;

namespace MAUIClientUI.UserInterface;

public partial class MainPageNotes : ContentPage
{
    private MainPageViewModel _viewModel;

    private ILogger _logger;
    public MainPageNotes()
    {
        var _noteServices = new DummyNoteServices();
        var _noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
        var dialogHelper = IPlatformApplication.Current.Services.GetService<iDialogHelper>();
        var _authService = IPlatformApplication.Current.Services.GetService<IAuthenticationService>();

        InitializeComponent();
        _viewModel = new MainPageViewModel(_noteServices, _noteRepository, dialogHelper, _authService);

        this.BindingContext = _viewModel;

        var loggerFactory = IPlatformApplication.Current.Services.GetService<ILoggerFactory>();
        _logger = loggerFactory?.CreateLogger<MainPageNotes>();
        LoadData();
    }

    private async void LoadData()
    {
        await _viewModel.LoadNotesAsync();

    }
    #region Just navigation to other elements

    #endregion
    protected override async void OnAppearing()
    {
        await _viewModel.LoadNotesAsync();
    }
}