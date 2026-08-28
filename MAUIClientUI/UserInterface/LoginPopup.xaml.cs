using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.MVVM;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using MAUIClientUI.Services.HelperClasses;
using MAUIClientUI.Services.ServerRequests;
using SlackAPI;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MAUIClientUI.UserInterface;

public partial class LoginPopup : ContentPage
{
    private LoginViewModel _loginViewModel;
    public LoginPopup()
    {
        InitializeComponent();
       
        var _authService = IPlatformApplication.Current.Services.GetService<IAuthenticationService>();
        var userServices = new UserServices("/api/user");

        _loginViewModel = new LoginViewModel(userServices, _authService);
        _loginViewModel.OnLoginSuccessful += OnLoginSuccess;

        BindingContext = _loginViewModel;

    }
    private async void OnLoginSuccess()
    {
        await Task.Delay(500);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnCreateAccountTapped(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new RegisterPopup());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loginViewModel.OnLoginSuccessful -= OnLoginSuccess;
    }
}
