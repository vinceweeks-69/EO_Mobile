using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace EOMobile
{
    public class EOBaseTabbedPage : TabbedPage
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
                        if (!Navigation.NavigationStack.Any(p => p is HelpPage))
                        {
                            TaskAwaiter t = Navigation.PushAsync(new HelpPage(commandParam)).GetAwaiter();
                        }
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
            while (Navigation.NavigationStack.Count > 1)
            {
                Navigation.RemovePage(Navigation.NavigationStack[0]);
            }

            while (Navigation.ModalStack.Count > 0)
            {
                Navigation.PopModalAsync();
            }
        }
    }
}
