using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using SlackAPI;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MAUIClientUI.UserInterface;

public partial class LoginPopup : ContentPage
{
    private UserServices _loginServices; // might delete this
    //private ClientServices _clientServices;
    private NoteServices _noteServices;
    private NoteRepository _noteRepository;
    private readonly IAuthenticationService _authService;

    public LoginPopup()
    {
        InitializeComponent();
        _noteRepository = IPlatformApplication.Current.Services.GetService<NoteRepository>(); // should DbContext be singleton
        _authService = IPlatformApplication.Current.Services.GetService<IAuthenticationService>();
        _loginServices = new UserServices("/api/user");
        _noteServices = new NoteServices("/api/notes");

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
        var result = await _loginServices.Login(username, password);
        SetLoadingState(true);

        if (result.IsSuccess)
        {
         

            // update notes based on changes on the server
            _authService.OnLoginSuccess(result.Data.IdUser);

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
