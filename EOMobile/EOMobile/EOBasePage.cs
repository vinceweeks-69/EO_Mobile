using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace EOMobile
{
    public class EOBasePage : ContentPage
    {
        public void Help_Clicked(object sender, EventArgs e)
        {
            Button b = sender as Button;

            if (b != null)
            {
                if (b.CommandParameter != null)
                {
                    string commandParam = b.CommandParameter as string;
                    if (commandParam != null && !String.IsNullOrEmpty(commandParam))
                    {
                        TaskAwaiter t = Navigation.PushAsync(new HelpPage(commandParam)).GetAwaiter();
                    }
                }
            }
        }

        public void Logout_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new LoginPage());
            ClearNavigationStacks();
        }

        public void ClearNavigationStacks()
        {
            while(Navigation.NavigationStack.Count > 1)
            {
                 Navigation.RemovePage(Navigation.NavigationStack[0]);
            }

            while(Navigation.ModalStack.Count > 0)
            {
                Navigation.PopModalAsync();
            }
        }
    }
}
