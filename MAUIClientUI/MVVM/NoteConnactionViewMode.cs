using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.MVVM
{
    // misspell cant rename because of conflicts
    public partial class NoteConnectionViewModel : ObservableObject
    {

        #region Properties
        [ObservableProperty]
        private string idNoteRequested;
        [ObservableProperty]
        private Guid _noteId;
        #endregion

        #region Services/Repositories
        private INoteServices _noteServices;
        private iDialogHelper _dialogHelper;
        private INavigationHelper _navigationHelper;

        #endregion

        #region events

        #endregion

        public NoteConnectionViewModel(INoteServices noteServices, iDialogHelper iDialogHelper, INavigationHelper navigationHelper
            , Guid IdNote)
        {
            _noteServices = noteServices;
            _dialogHelper = iDialogHelper;
            _navigationHelper = navigationHelper;   

            _noteId = IdNote;
        }

        [RelayCommand]
        private async Task AccessNoteClicked()
        {
            if (string.IsNullOrWhiteSpace(idNoteRequested))
            {
                await _dialogHelper.ShowAlertAsync("Error", "Please enter a Note ID", "OK");
                return;
            }

            if (!Guid.TryParse(idNoteRequested, out var userId))
            {
                await _dialogHelper.ShowAlertAsync("Error", "Invalid User ID format. Please enter a valid GUID.", "OK");
                return;
            }

            try
            {
                var result = await _noteServices.GiveNoteAccessToUser(_noteId, userId);
                if (result.IsSuccess)
                {
                    await _navigationHelper.PopAsync(); 
                }
                else
                {
                    await _dialogHelper.ShowAlertAsync("Error", "Failed to find User. Please check the ID and try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogHelper.ShowAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}
