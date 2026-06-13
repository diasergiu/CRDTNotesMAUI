using CRDT_TestShering.Services;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CRDT_TestShering.UserInterface;

public partial class LoginPopup : ContentPage
{
    private LoginConnectionServices loginServices;

    public LoginPopup()
    {
        InitializeComponent();
        loginServices = new LoginConnectionServices();        
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
        var result = await _apiService.LoginAstnc(
            username,
            password
        );

        if( !result.IsSuccess){
            // Display the error message from the service
            ShowStatus(result.ErrorMessage, true);
        }

        SetLoadingState(false);

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
