using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CRDT_TestShering.UserInterface;

public partial class RegisterPopup : ContentPage
{
    private readonly HttpClient _httpClient;

    public RegisterPopup()
    {
        InitializeComponent();

        // Configure HttpClient for local server
#if DEBUG
        var baseUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5000"  // Android emulator
            : "http://localhost:5000"; // Windows/iOS/Mac
#else
        var baseUrl = "https://your-production-api.com";
#endif

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
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

        try
        {
            var registerRequest = new
            {
                name = name,
                username = username,
                password = password
            };

            var json = JsonSerializer.Serialize(registerRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/Register", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var registerResponse = JsonSerializer.Deserialize<RegisterResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (registerResponse?.Success == true)
                {
                    ShowStatus("Account created successfully!", false);
                    await Task.Delay(1500);

                    // Navigate back to login
                    await Navigation.PopModalAsync();
                }
                else
                {
                    ShowStatus(registerResponse?.Message ?? "Registration failed", true);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                ShowStatus("Username already exists", true);
            }
            else
            {
                ShowStatus($"Registration failed: {response.StatusCode}", true);
            }
        }
        catch (HttpRequestException ex)
        {
            ShowStatus($"Connection error: {ex.Message}", true);
        }
        catch (TaskCanceledException)
        {
            ShowStatus("Request timeout. Is the server running?", true);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", true);
        }
        finally
        {
            RegisterButton.IsEnabled = true;
            RegisterButton.Text = "Register";
        }
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
