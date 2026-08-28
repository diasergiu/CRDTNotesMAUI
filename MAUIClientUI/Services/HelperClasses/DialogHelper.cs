using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services.HelperClasses
{
    public class DialogHelper : iDialogHelper
    {
        private Page CurrentPage =>
            Application.Current?.Windows[0]?.Page
            ?? throw new InvalidOperationException("No active page.");

        public Task ShowAlertAsync(string title, string message, string cancel = "OK")
            => CurrentPage.DisplayAlert(title, message, cancel);

        public Task<bool> ShowConfirmationAsync(string title, string message,
            string accept = "Yes", string cancel = "No")
            => CurrentPage.DisplayAlert(title, message, accept, cancel);
    }
}
