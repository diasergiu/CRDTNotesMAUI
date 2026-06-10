using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CRDT_TestShering.UserInterface;

public partial class LoginPopup : ContentPage
{
    private readonly HttpClient _httpClient;

    public LoginPopup()
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

        // Disable button and show loading
        LoginSubmitButton.IsEnabled = false;
        LoginSubmitButton.Text = "Logging in...";
        StatusLabel.IsVisible = false;

        try
        {
            // Make GET request to server (for testing)
            var response = await _httpClient.GetAsync($"/api/test?username={Uri.EscapeDataString(username)}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                ShowStatus($"Success! Response: {content}", false);

                // Wait a moment then close the popup
                await Task.Delay(1500);
                await Navigation.PopModalAsync();
            }
            else
            {
                ShowStatus($"Login failed: {response.StatusCode}", true);
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
            LoginSubmitButton.IsEnabled = true;
            LoginSubmitButton.Text = "Login";
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = isError ? Colors.Red : Colors.Green;
        StatusLabel.IsVisible = true;
    }
}
