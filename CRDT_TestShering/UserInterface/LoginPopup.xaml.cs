using CRDT_TestShering.Services;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CRDT_TestShering.UserInterface;

public partial class LoginPopup : ContentPage
{
    private ServicesConnectionLayer _apiService;

    public LoginPopup()
    {
        InitializeComponent();
        _apiService = new ServicesConnectionLayer();        
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

        // Handle the result
        //if (result.IsSuccess)
        //{
        //    // Save authentication
        //    await _authService.SaveLoginAsync(
        //        result.Data.AuthToken,
        //        result.Data.User.IdUser,
        //        result.Data.User.Username
        //    );

        //    // TODO: Save notes to local database
        //    // await SaveNotesToLocalDb(result.Data.Notes);

        //    ShowStatus("Login successful!", false);
        //    await Task.Delay(1000);
        //    await Navigation.PopModalAsync();
        //}
        //else
        if( !result.IsSuccess){
            // Display the error message from the service
            ShowStatus(result.ErrorMessage, true);

            // Optional: Handle different error types differently
            if (result.ErrorType == ApiErrorType.ConnectionError)
            {
                // Maybe show a "Retry" button or different UI
            }
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
