using CRDT_TestShering.Services;
using DatabaseLibrary.Services;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CRDT_TestShering.UserInterface;

public partial class RegisterPopup : ContentPage
{
    private readonly RegisterConnectionServices _registerServices;

    public RegisterPopup()
    {
        InitializeComponent();

        _registerServices = new RegisterConnectionServices(BaseURLGetter.getBaseURL());
        
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;

        // Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowStatus("Please enter your name", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowStatus("Please enter a username", true);
            return;
        }

        if (username.Length < 3)
        {
            ShowStatus("Username must be at least 3 characters", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowStatus("Please enter a password", true);
            return;
        }

        if (password.Length < 6)
        {
            ShowStatus("Password must be at least 6 characters", true);
            return;
        }

        if (password != confirmPassword)
        {
            ShowStatus("Passwords do not match", true);
            return;
        }

        // Disable button and show loading
        RegisterButton.IsEnabled = false;
        RegisterButton.Text = "Creating account...";
        StatusLabel.IsVisible = false;

        var result = await _registerServices.RegisterNewUser(name, username, password);

        if (result.IsSuccess)
        {
            ShowStatus("Account created successfully!", false);
            await Task.Delay(1500);

            // Navigate back to login
            await Navigation.PopModalAsync();
        }
        else
        {
            ShowStatus(result.ErrorMessage ?? "Registration failed", true);
        }

        // Re-enable button and reset text
        RegisterButton.IsEnabled = true;
        RegisterButton.Text = "Register";
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = isError ? Colors.Red : Colors.Green;
        StatusLabel.IsVisible = true;
    }

    private class RegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserInfo User { get; set; }
    }

    private class UserInfo
    {
        public int IdUser { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
    }
}
