using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using MAUIClientUI.MVVM;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;

namespace MAUIClientUI.UserInterface;

public partial class MainPageNotes : ContentPage
{
    private NotesViewModel _viewModel;
    private readonly NoteRepository _noteRepository;
    private INoteServices _noteServices;
    private readonly IAuthenticationService _authService;
    private Guid _currentUserId;
    private bool _isLoggedIn = false;

    public MainPageNotes()
    {
        _noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
        _authService = IPlatformApplication.Current.Services.GetService<IAuthenticationService>();

        InitializeComponent();
        _viewModel = new NotesViewModel();
        this.BindingContext = _viewModel;
        this._noteServices = new DummyNoteServices();
        _authService.LoginSucceeded += OnLoginSucceeded;
        LoadData();
    }

    private async void LoadData()
    {
       
        // Load notes from database
        await _viewModel.LoadNotesAsync();
       
    }
    #region Just navigation to other elements
    private async void OnOpenNoteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is NoteClient note)
        {
            await Navigation.PushAsync(new NoteView(note, _noteServices));
        }
    }

    private async void OnGetAccessToNotesClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new NoteConnectionPopup());
    }


    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var loginPopup = new LoginPopup();
        await Navigation.PushModalAsync(loginPopup);
    }
    #endregion
    private async void OnCreateNoteClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new NoteView(_noteServices));
    }

    protected override async void OnAppearing()
    {
        await _viewModel.LoadNotesAsync();       
    }
    private async void OnSyncNotesClicked(object sender, EventArgs e)
    {
        if (!_isLoggedIn)
        {
            await DisplayAlert("Not Logged In", "Please log in first to sync notes.", "OK");
            return;
        }

        var notesResult = await _noteServices.GetAllNotesFromUser(_currentUserId);
        if (notesResult.IsSuccess)
        {
            // Update the local repository with the notes from the server
            _noteRepository.UpdateListNotes(notesResult.Data);
            LoadData();
            await DisplayAlert("Success", "Notes synced from server!", "OK");
        }
        else
        {
            await DisplayAlert("Error", "Failed to sync notes from server.", "OK");
        }

        var charResults = await _noteServices.GetAllCharacterByUser();
        if (charResults.IsSuccess)
        {
            // Update the local repository with the notes from the server
            _noteRepository.saveCRDTChanges(charResults.Data.Select(a => new CRDTCharacterClient(a)).ToList());
        }
    }

    private async void OnLoginSucceeded(object? sender, Guid userId)
    {
        // ✅ Re-create the service with real implementation
        this._noteServices = new NoteServices("/api/notes", _noteRepository);
        _currentUserId = userId;
        _isLoggedIn = true;

        // Enable the sync and NoteAccess buttons
        SyncNotesButton.IsEnabled = true;
        GetAccessToNotesButton.IsEnabled = true;
        var clientChanges = _noteRepository.GetNoteFromUser(userId);
        var result = _noteServices.SendChangesToServer(clientChanges);

        var notesResult = await _noteServices.GetAllNotesFromUser(userId);
        if (notesResult.IsSuccess)
        {
            // Update the local repository with the notes from the server
            _noteRepository.UpdateListNotes(notesResult.Data);
        }

        var offlineChanges = await _noteRepository.GetAllCRDTCharacters();

        var crdtChangesResult = await _noteServices.SendCRDTChangestoServer(prepperCRDTForServer(offlineChanges));
        if (crdtChangesResult.IsSuccess)
        {
            await _noteRepository.ClearDirtyFlag(offlineChanges);

        }
        var charResults = await _noteServices.GetAllCharacterByUser();
        if (charResults.IsSuccess)
        {
            // Update the local repository with the Charactrers from the server
            _noteRepository.saveCRDTChanges(charResults.Data.Select(a => new CRDTCharacterClient(a)).ToList());
        }
    }


    private List<CRDTCharacter> prepperCRDTForServer(List<CRDTCharacterClient> toChange)
    {
        List<CRDTCharacter> result = new List<CRDTCharacter>();
        foreach (var item in toChange)
        {
            result.Add(new CRDTCharacter(item));
        }
        return result;
    }
}