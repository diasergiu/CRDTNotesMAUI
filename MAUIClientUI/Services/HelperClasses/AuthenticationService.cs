using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using Microsoft.Extensions.Logging;
using System;

namespace MAUIClientUI.Services.HelperClasses
{
    internal class AuthenticationService : IAuthenticationService
    {
        private readonly IUserContext _userContext;
        private readonly ILogger<AuthenticationService>? _logger;

        public UserClient? CurrentUser { get; private set; }

        public bool IsLoggedIn => CurrentUser != null;

        public event EventHandler<Guid>? LoginSucceeded;

        public AuthenticationService(IUserContext userContext, ILogger<AuthenticationService>? logger = null)
        {
            _userContext = userContext;
            _logger = logger;
        }

        public void OnLoginSuccess(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                _logger?.LogWarning("OnLoginSuccess called with empty user id; ignoring.");
                return;
            }

            try
            {
                // Initialize the shared session (IUserContext) for the freshly logged-in user.
                _userContext.LocalUser = userId;
                _userContext.HubConnectionId = null;
                CurrentUser = new UserClient { IdUser = userId };

                LoginSucceeded?.Invoke(this, userId);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error during login success handling");
            }
        }

        public void Logout()
        {
            CurrentUser = null;
            _userContext.LocalUser = Guid.Empty;
            _userContext.HubConnectionId = null;
        }
    }
}
