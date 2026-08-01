using DatabaseLibrary.Entities.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services
{
    public interface IAuthenticationService
    {
        Guid idUser { get; set; }
        UserClient? CurrentUser { get; set; }
        bool IsLoggedIn();
        void Logout();
        // later probably make it work with User
        public void OnLoginSuccess(Guid idUser);

        event EventHandler<Guid>? LoginSucceeded;
    }
}
