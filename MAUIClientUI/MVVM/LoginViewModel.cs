using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Services.HelperClasses;
using MAUIClientUI.Services.ServerRequests;

namespace MAUIClientUI.MVVM
{
    public partial class LoginViewModel : ObservableObject
    {
        #region Properties
        [ObservableProperty]
        private string username = string.Empty; // i dont know if i need this, it is the conventional way but i dont undestand the conventional way of dooing thigs here
        [ObservableProperty]
        private string password = string.Empty;
        [ObservableProperty]
        private string statusMessage = string.Empty;
        [ObservableProperty]
        private bool isLoading = false;
        [ObservableProperty]
        private bool isError = false;
        #endregion
        #region Services/Repositories
        private UserServices _loginServices;
        private IAuthenticationService _authService;
        #endregion
        #region events
        #endregion

        public LoginViewModel(UserServices userServices, IAuthenticationService authService)
        {
            _loginServices = userServices;
            _authService = authService;
        }
        #region commands
        [RelayCommand]
        private async Task LoginAsync()
        {
            // Validation happens in ViewModel
            if (string.IsNullOrWhiteSpace(Username))
            {
                ShowError("Please enter a username");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Please enter a password");
                return;
            }

            IsLoading = true;

            try
            {
                // Pass credentials to service - business logic handled here
                var result = await _loginServices.Login(Username, Password);

                if (result.IsSuccess)
                {
                    // Call auth service to update state
                    _authService.OnLoginSuccess(result.Data.IdUser);
                    ShowSuccess("Login successful!");

                    // Optional: Raise an event to signal the View to close
                    OnLoginSuccessful?.Invoke();
                }
                else
                {
                    ShowError(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                ShowError($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region Methods
        private void ShowError(string message)
        {
            StatusMessage = message;
            IsError = true;
        }

        private void ShowSuccess(string message)
        {
            StatusMessage = message;
            IsError = false;
        }
        #endregion

        #region Events
        public event Action OnLoginSuccessful;
        #endregion

    }
}
