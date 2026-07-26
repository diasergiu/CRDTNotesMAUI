using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using MAUIClientUI.MVVM;
using MAUIClientUI.Repositories;

namespace MAUIClientUI.UserInterface;

public partial class MainPageNotes : ContentPage
{
	private NotesViewModel _viewModel;
	private readonly NoteRepository _noteRepository;
    private readonly IDatabaseServices _databaseService;

    public MainPageNotes()
	{
		_noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
        _databaseService = IPlatformApplication.Current.Services.GetService<IDatabaseServices>();
        InitializeComponent();
		_viewModel = new NotesViewModel();
		this.BindingContext = _viewModel;

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
			await Navigation.PushAsync(new NoteView(note));
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
		await Navigation.PushAsync(new NoteView(newNote, isNewNote: true));
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
}