using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services.HelperClasses
{
    public interface INavigationHelper
    {
        Task PopAsync();
        Task PushAsync(Page page);
        Task PopModalAsync();
        Task PushModalAsync(Page page);
    }
}
