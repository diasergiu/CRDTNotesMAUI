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
    private readonly IDatabaseServices _databaseService;
    private INoteServices _noteServices;
    private readonly IAuthenticationService _authService;

    public MainPageNotes()
	{
		_noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
        _databaseService = IPlatformApplication.Current.Services.GetService<IDatabaseServices>();
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
		
        using (var dbContext = _databaseService.GetContext())
		{
			// Ensure database is created
			await dbContext.Database.EnsureCreatedAsync();

			// Load notes from database
			await _viewModel.LoadNotesAsync(dbContext);
		}
	}

	private async void OnOpenNoteClicked(object sender, EventArgs e)
	{
		if (sender is Button button && button.CommandParameter is NoteClient note)
		{
			await Navigation.PushAsync(new NoteView(note, _noteServices));
		}
	}

	private async void OnCreateNoteClicked(object sender, EventArgs e)
	{
		var newNote = new NoteClient
		{
			Title = "",
			Content = "",
			CreationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
			LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
		};
		await Navigation.PushAsync(new NoteView(newNote,_noteServices, isNewNote: true));
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		using (var dbContext = _databaseService.GetContext())
		{
			await _viewModel.LoadNotesAsync(dbContext);
		}
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		var loginPopup = new LoginPopup();
		await Navigation.PushModalAsync(loginPopup);

	}
	private async void OnLoginSucceeded(object? sender, Guid userId)
	{
		// ✅ Re-create the service with real implementation
		this._noteServices = new NoteServices("/api/notes");

		var clientChanges = _noteRepository.GetNoteFromUser(userId);
		var result = _noteServices.SendChangesToServer(clientChanges);

		var notesResult = await _noteServices.GetAllNotesFromUser(userId);
		if (notesResult.IsSuccess)
		{
			// Update the local repository with the notes from the server
			_noteRepository.UpdateListNotes(notesResult.Data);
		}
	}
}