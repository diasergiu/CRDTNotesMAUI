//using Android.App;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;
using MAUIClientUI.Services.ServerRequests;
using MAUIClientUI.UserInterface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MAUIClientUI.MVVM
{
    public partial class MainPageViewModel : ObservableObject
    {
        #region Porperties
        [ObservableProperty]
        private bool isLoggedIn = false;
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
            isLoggedIn = true; // this shuold change syncButton to enabled

            var clientChanges = _noteRepository.GetNoteFromUser(userId);
            var result = _noteServices.SendChangesToServer(clientChanges);

            var offlineChanges = await _noteRepository.GetAllCRDTCharacters();

            var crdtChangesResult = await _noteServices.SendCRDTChangestoServer(prepperCRDTForServer(offlineChanges));
            if (crdtChangesResult.IsSuccess)
            {
                await _noteRepository.ClearDirtyFlag(offlineChanges);

            }

            GetChangesFromServer();
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
            var notesResult = await _noteServices.GetAllNotesFromUser(UserDevice.LocalUser);


            var charResults = await _noteServices.GetAllCharacterByUser();
            if (notesResult.IsSuccess)
            {
                // Update the local repository with the notes from the server
                _noteRepository.UpdateListNotes(notesResult.Data);

                await _dialogHelper.ShowAlertAsync("Success", "Notes synced from server!", "OK");
            }
            if (charResults.IsSuccess && notesResult.IsSuccess)
            {
                // Update the local repository with the notes from the server
                await _noteRepository.SaveCRDTChanges(charResults.Data.Select(a => new CRDTCharacterClient(a)).ToList());
            }
            else
            {
                await _dialogHelper.ShowAlertAsync("Error", "Failed to sync notes from server.", "OK");
            }
            await LoadNotesAsync();
        }

        private List<CRDTCharacter> prepperCRDTForServer(List<CRDTCharacterClient> toChange)
        {
            List<CRDTCharacter> result = new List<CRDTCharacter>();
            foreach (var item in toChange)
            {
                result.Add(new CRDTCharacter()
                {
                    Character = item.Character,
                    IdCharacter = string.IsNullOrEmpty(item.IdCharacter)
                        ? item.IdCharacter
                        : CharacterIdProtector.Encrypt(item.IdCharacter),
                    IdNote = item.IdNote,
                    Operation = item.Operation,
                    ClockDateTime = item.ClockDateTime,
                    Tombstone = item.Tombstone
                });
            }
            return result;
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
