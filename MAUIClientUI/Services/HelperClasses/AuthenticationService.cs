using DatabaseLibrary.Entities.Client;
using SlackAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services.HelperClasses
{
    internal class AuthenticationService : IAuthenticationService
    {
        public UserClient? CurrentUser { get; set; }
        public Guid idUser { get; set; }

        public event EventHandler<Guid>? LoginSucceeded;

        public bool IsLoggedIn()
        {
            return CurrentUser != null;
        }

        public void OnLoginSuccess(Guid userId)
        {
            this.idUser = userId;
            LoginSucceeded?.Invoke(this, idUser);

        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
