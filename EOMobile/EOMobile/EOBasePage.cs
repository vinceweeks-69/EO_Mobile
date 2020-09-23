using EOMobile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace EOMobile
{
    public class EOBasePage : ContentPage 
    {
        public async void StartCamera()
        {
            try
            {
                var action = await DisplayActionSheet("Add Photo", "Cancel", null, "Choose Existing", "Take Photo");

                if (action == "Choose Existing")
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var fileName = ((App)App.Current).SetImageFileName();
                        DependencyService.Get<ICameraInterface>().LaunchGallery(FileFormatEnum.JPEG, fileName);
                    });
                }
                else if (action == "Take Photo")
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var fileName = ((App)App.Current).SetImageFileName();
                        DependencyService.Get<ICameraInterface>().LaunchCamera(FileFormatEnum.JPEG, fileName);
                    });
                }
            }
            catch (Exception ex)
            {

            }
        }

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

        public bool PopToPage(string targetPageName)
        {
            bool targetPageFound = false;

            Type t = Type.GetType("EOMobile." + targetPageName);
            object o = Activator.CreateInstance(t);

            foreach (Page p in Navigation.NavigationStack)
            {
                if(p.GetType() ==  o.GetType())
                {
                    targetPageFound = true;
                }
            }

            if (targetPageFound)
            {
                int pagesToPop = 0;

                for (int a =  Navigation.NavigationStack.Count - 1;  a >= 0;  a--)
                {
                    if (Navigation.NavigationStack[a].GetType() == o.GetType())
                        break;
                    else
                        pagesToPop++;
                }

                for(int a = 0; a < pagesToPop; a++)
                {
                    Navigation.PopAsync();
                } 
            }

            return targetPageFound;
        }
    }
}
