using System;
using System.Collections.Generic;
using System.Text;

namespace MAUIClientUI.Services.HelperClasses
{
    public class NavigationHelper : INavigationHelper
    {
        public Task PopAsync() => Shell.Current.Navigation.PopAsync();
        public Task PushAsync(Page page) => Shell.Current.Navigation.PushAsync(page);
        public Task PopModalAsync() => Shell.Current.Navigation.PopModalAsync();
        public Task PushModalAsync(Page page) => Shell.Current.Navigation.PushModalAsync(page);
    }
}
