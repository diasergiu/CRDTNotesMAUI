using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services.HelperClasses
{
    public interface iDialogHelper
    {
        Task ShowAlertAsync(string title, string message, string cancel = "OK");
        Task<bool> ShowConfirmationAsync(string title, string message, string accept = "yes", string cancel = "No");
    }
}
