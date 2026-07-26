using MAUIClientUI.Services;
using MAUIClientUI.Repositories;
using DatabaseLibrary.Entities;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;

namespace MAUIClientUI.UserInterface;

public partial class LoginPopup : ContentPage
{
    //private LoginConnectionServices loginServices; // might delete this
    private ClientNoteServices noteServices;
    private NoteRepository noteRepository;

    public LoginPopup()
    {
        InitializeComponent();
        noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>(); // should DbContext be singleton
        noteServices = new ClientNoteServices("/api/user");
    }

    private async void OnLoginSubmitClicked(object sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        // Validation
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowStatus("Please enter a username", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowStatus("Please enter a password", true);
            return;
        }
        
        SetLoadingState(false);
        // Login to the server
        // lets pretend this korks
        var result = noteServices.Login(username, password);

        // Show loading state
        SetLoadingState(true);

        // Call the service - NO TRY/CATCH needed!



        if (result.IsSuccess)
        {
     //       UserDevice.SaveLastUserToFile(result.Data);
            // sync the notes with the server
            List<SyncQueueClient> changedNotes = noteRepository.getAllChanges(result.Data);
            var getServerChanges = await noteServices.SendAndReceiveNoteUpdates(
                changedNotes,
                result.Data
            );
            
            // update notes based on changes on the server
            noteRepository.UpdateListNotes(getServerChanges.Data);
            // Login successful - close the popup
            ShowStatus("Login successful!", false);
            await Task.Delay(500); // Brief delay to show success message
            await Navigation.PopModalAsync();
        }
        else
        {
            // Display the error message from the service
            ShowStatus(result.ErrorMessage, true);

        }

    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnCreateAccountTapped(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new RegisterPopup());
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = isError ? Colors.Red : Colors.Green;
        StatusLabel.IsVisible = true;
    }

    private void SetLoadingState(bool isLoading)
    {
        LoginSubmitButton.IsEnabled = !isLoading;
        LoginSubmitButton.Text = isLoading ? "Logging in... " : "Login";
        StatusLabel.IsVisible = false;
    }
}
