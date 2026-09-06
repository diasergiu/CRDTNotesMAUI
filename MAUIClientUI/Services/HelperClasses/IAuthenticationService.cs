using DatabaseLibrary.Entities.Client;
using System;

namespace MAUIClientUI.Services.HelperClasses
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// The currently logged-in user (domain object), if any.
        /// </summary>
        UserClient? CurrentUser { get; }

        bool IsLoggedIn { get; }

        /// <summary>
        /// Called by the login flow after the server confirms the user id.
        /// The service initializes the shared IUserContext (session) and raises LoginSucceeded.
        /// </summary>
        void OnLoginSuccess(Guid userId);

        void Logout();

        event EventHandler<Guid>? LoginSucceeded;
    }
}
