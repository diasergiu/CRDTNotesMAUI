using DatabaseLibrary.Entities.Client;
using MAUIClientUI.UserInterface;
using Microsoft.Extensions.Logging;
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
        private ILogger<AuthenticationService> _logger;

        public event EventHandler<Guid>? LoginSucceeded;

        public AuthenticationService()
        {
            var loggerFactory = IPlatformApplication.Current.Services.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<AuthenticationService>();
        }

        public bool IsLoggedIn()
        {
            return CurrentUser != null;
            _logger?.LogInformation("we are logging in ");
        }

        public void OnLoginSuccess(Guid userId)
        {
            try
            {
                this.idUser = userId;
                LoginSucceeded?.Invoke(this, idUser);
            }
            catch(Exception e)
            {
                _logger?.LogInformation(e.Message);
            }

        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
