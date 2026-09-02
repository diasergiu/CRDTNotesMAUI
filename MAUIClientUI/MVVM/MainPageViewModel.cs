//using Android.App;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;
using MAUIClientUI.Services.ServerRequests;
using MAUIClientUI.UserInterface;
using SlackAPI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MAUIClientUI.MVVM
{
    public partial class MainPageViewModel : ObservableObject
    {
        #region Porperties
        [ObservableProperty]
        private bool isLoggedIn = false;
        [ObservableProperty]
        private string textLogin = "Login";
        #endregion
        #region Dependencies
        private NoteRepository _noteRepository;
        private INoteServices _noteServices;
        private iDialogHelper _dialogHelper;
        private IAuthenticationService _authService;

        #endregion

        #region Events
        private ObservableCollection<NoteClient> _listOfNotes = new ObservableCollection<NoteClient>();
        private ObservableCollection<CRDTCharacter> _notes = new ObservableCollection<CRDTCharacter>();

        #endregion
        public ObservableCollection<NoteClient> ListOfNotes
        {
            get => _listOfNotes;
            set
            {
                _listOfNotes = value;
                OnPropertyChanged();
            }
        }

        public MainPageViewModel(INoteServices noteServices, NoteRepository noteRepository, iDialogHelper dialogHelper, IAuthenticationService authService)
        {
            _noteServices = noteServices;
            _noteRepository = noteRepository;
            _dialogHelper = dialogHelper;
            _authService = authService;

            _authService.LoginSucceeded += OnLoginSucceeded;
        }

        public async Task LoadNotesAsync()
        {
            var notes = await _noteRepository.GetAllNotes();
            ListOfNotes.Clear();
            foreach (var note in notes)
            {
                ListOfNotes.Add(note);
            }
        }

        [RelayCommand]
        public async void SyncNotesClick()
        {
            GetChangesFromServer();
        }
        private async void OnLoginSucceeded(object? sender, Guid userId) // need to do this before testing SyncData
        {
            this._noteServices = new NoteServices("/api/notes", _noteRepository); // because of this
            IsLoggedIn = true; // this shuold change syncButton to enabled
            TextLogin = "Logout";
            SendOfflineChanges(userId);
            GetChangesFromServer();
        }
        [RelayCommand]
        public async Task AuthenticationButtonClicked()
        {
            if (IsLoggedIn)
            {
                // User is logged in, so logout
                this._noteServices = new DummyNoteServices();
                IsLoggedIn = false;
                TextLogin = "Login";

                await _dialogHelper.ShowAlertAsync("Logout", "You have been logged out.", "OK");
            }
            else
            {
                // User is not logged in, show login popup
                var loginPopup = new LoginPopup();
                await Shell.Current.Navigation.PushModalAsync(loginPopup);
            }
        }

        private async void SendOfflineChanges(Guid userId)
        {
            var clientChanges = _noteRepository.GetNoteFromUser(userId);
            var prepareToSend = PrepareChanges(clientChanges);
            var result = await _noteServices.SendChangesToServer(prepareToSend);
            if (result.IsSuccess)
            {
                await _noteRepository.ClearDirtyFlag(clientChanges);
                await _noteRepository.DeleteNotesWithIdDeleted();
                await _noteRepository.UpdateBasedOnNoteServer(result.Data);
            }
        }

        // those should probably not be here but _noteServices changes after login 
        [RelayCommand]
        private async Task OpenNoteAsync(NoteClient note)
        {
            if (note is null)
                return;

            // Uses the VM's _noteServices — real one after login
            await Shell.Current.Navigation.PushAsync(new NoteView(note, _noteServices));
        }
        [RelayCommand]
        private async Task CreateNoteAsync()
        {
            // Uses the VM's _noteServices — real one after login
            await Shell.Current.Navigation.PushAsync(new NoteView(_noteServices));
        }
        private async void GetChangesFromServer()
        {
            try
            {
                //var notesResult = await _noteServices.GetAllNotesFromUser(UserDevice.LocalUser);
                var serverChangesResult = await _noteServices.GetServerChanges();

                //if (notesResult.IsSuccess)
                //{
                //    // Update the local repository with the notes from the server
                //    _noteRepository.UpdateListNotes(notesResult.Data);

                //}

                if (serverChangesResult.IsSuccess && serverChangesResult.Data != null)
                {
                   // Decode CRDT changes from payloads
                   //var decodedChanges = new List<CRDTCharacterClient>();
                   // foreach (var change in serverChangesResult.Data)
                   // {
                   //     var decodedCharacters = CharacterSerializer.Decode(change.Payload);

                   //     foreach (var character in decodedCharacters)
                   //     {
                   //         decodedChanges.Add(new CRDTCharacterClient
                   //         {
                   //             IdCharacter = character.IdCharacter,
                   //             Character = character.Character,
                   //             Tombstone = character.Tombstone,
                   //             IdNote = change.NoteServer.IdNote,
                   //             IsDirtyFlag = true,
                   //         });

                   //     }
                       

                   // }

                   // // Update the local repository with decoded CRDT changes
                   // await _noteRepository.SaveCRDTChanges(decodedChanges);
                    await _noteRepository.UpdateBasedOnNoteServer(serverChangesResult.Data);
                }
                else if (!serverChangesResult.IsSuccess)
                {
                    await _dialogHelper.ShowAlertAsync("Error", "Failed to sync notes from server.", "OK");
                }
                await _dialogHelper.ShowAlertAsync("Success", "Notes synced from server!", "OK");

                await LoadNotesAsync();
            }
            catch (Exception ex)
            {
                await _dialogHelper.ShowAlertAsync("Error", $"An error occurred while syncing notes: {ex.Message}", "OK");
            }
        }

        private List<DTOSendChanges> PrepareChanges(List<NoteClient> listNoteChanges)
        {
            List<DTOSendChanges> changes = new List<DTOSendChanges>();
            foreach (var note in listNoteChanges)
            {
                changes.Add(new DTOSendChanges
                {
                    NoteServer = EntityMapper.MapNoteClientToNoteServer(note),
                    Payload = CharacterSerializer.Encode(note.CRDTCharacter)
                });
            }
            return changes;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #region Events 


        #endregion
    }
}
