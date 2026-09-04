
using DatabaseLibrary.Entities.Client;
using MAUIClientUI.Miscellaneous;
using MAUIClientUI.MVVM;
#if ANDROID
using MAUIClientUI.Platforms.Android;
#elif WINDOWS
using MAUIClientUI.Platforms.Windows;
#endif
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;

namespace MAUIClientUI.UserInterface;

public partial class NoteView : ContentPage
{

    private readonly NotesViewModel _notesViewModel;
    private readonly NoteClient _currentNote;
    private IContentEditorInputHandler _inputHandler;

    public NoteView(INoteServices noteService)
    : this(new NoteClient()
    {
        Title = "",
        CreationDate = DateTime.Now,
        LastUpdate = DateTime.Now,
        DirtyFlagChangesMade = true,
        Version = 1
    }, noteService, true)
    {
        var noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>();
        noteRepository.CreateNote(_currentNote);
    }
    public NoteView(NoteClient note, INoteServices noteService, bool isNewNote = false)
    {
        InitializeComponent();
        _currentNote = note;

        _notesViewModel = new NotesViewModel(note, noteService, isNewNote);
        _notesViewModel.ContentRefreshRequested += OnContentRefreshRequested;

        BindingContext = _notesViewModel;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_notesViewModel.AppearingCommand.CanExecute(null))
            _notesViewModel.AppearingCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_notesViewModel.DisappearingCommand.CanExecute(null))
            _notesViewModel.DisappearingCommand.Execute(null);
    }


    /// <summary>
    /// Pushes the note content into the editor. Content is not data-bound because the
    /// collaborative (CRDT) editor is driven by per-keystroke operations, so the ViewModel
    /// raises an event and the View updates the control on the main thread.
    /// </summary>
    private void OnContentRefreshRequested(string text)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ContentEditor.Text = text;
        });
    }

    /// <summary>
    /// Wires the platform-specific input handler once the editor's native control exists.
    /// Keystrokes are forwarded to the ViewModel's NoteController; the CRDT model itself is
    /// never touched from the View.
    /// </summary>
    private void OnEditorHandlerChanged(object sender, EventArgs e)
    {

        if (sender is Editor editor && editor.Handler is not null)
        {
#if WINDOWS
        _inputHandler = new WindowsContentEditorHandler(ContentEditor);
        _inputHandler.CharacterInserted += _notesViewModel.NoteOrchestrator.InsertCharacter;
        _inputHandler.CharacterDeleted += _notesViewModel.NoteOrchestrator.DeleteCharacter;
        //_inputHandler.StringInserted += _notesViewModel.NoteController.InsertString;     
        //_inputHandler.RangeDeleted += _notesViewModel.NoteController.DeleteCharacterRange;
        var platformView = (Microsoft.UI.Xaml.Controls.TextBox)editor.Handler.PlatformView;
        if (platformView != null)
        {
            platformView.KeyDown += _inputHandler.HandleKeyPress;
            platformView.KeyUp += _inputHandler.HandleKeyUp;
            //platformView.TextChanged += _inputHandler.HandleTextChanged;
        }
#elif ANDROID
            _inputHandler = new AndroidContentEditorHandler(ContentEditor);
            _inputHandler.CharacterInserted += _notesViewModel.NoteOrchestrator.InsertCharacter;
            _inputHandler.CharacterDeleted += _notesViewModel.NoteOrchestrator.DeleteCharacter;
            //_inputHandler.StringInserted += _notesViewModel.NoteController.InsertString;
            //_inputHandler.RangeDeleted += _notesViewModel.NoteController.DeleteCharacterRange;
            var platformView = (Android.Widget.EditText)editor.Handler.PlatformView;
        if (platformView != null)
        {
            platformView.KeyPress += _inputHandler.HandleKeyPress;
        //    platformView.TextChanged += _inputHandler.HandleTextChanged;    
        }
#endif

        }
    }
}
