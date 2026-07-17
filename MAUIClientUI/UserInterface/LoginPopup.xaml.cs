using MAUIClientUI.Services;
using MAUIClientUI.Repositories;
using DatabaseLibrary.Entities;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MAUIClientUI.UserInterface;

public partial class LoginPopup : ContentPage
{
    //private LoginConnectionServices loginServices; // might delete this
    private ClientNoteServices noteServices;
    private NoteRepository noteRepository;

    public LoginPopup()
    {
        InitializeComponent();
        noteRepository = new NoteRepository(new DbContextClient()); // should DbContext be singleton
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

        // Show loading state
        SetLoadingState(true);

        // Call the service - NO TRY/CATCH needed!
        List<Note> changedNotes = noteRepository.getAllFlagedNotes();
        var result = await noteServices.SendAndReceiveNoteUpdates(
            changedNotes,
            username,
            password
        );

        // update notes based on changes on the server
        noteRepository.UpdateListNotes(changedNotes);
        SetLoadingState(false);

        if (result.IsSuccess)
        {
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
