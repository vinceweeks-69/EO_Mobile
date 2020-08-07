using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    namespace XamarinFormsCamera
    {
        public partial class CameraApp : Application
        {
            //Static variables for the app
            public static string DefaultImageId = "default_image";
            public static string ImageIdToSave = null;

            protected void InitializeComponent()
            {
                global::Xamarin.Forms.Xaml.Extensions.LoadFromXaml(this, typeof(CameraApp));
            }

            public CameraApp()
            {
                InitializeComponent();

                MainPage = new CameraPage();
            }

            protected override void OnStart()
            {
                // Handle when your app starts
            }

            protected override void OnSleep()
            {
                // Handle when your app sleeps
            }

            protected override void OnResume()
            {
                // Handle when your app resumes
            }
        }
    }

}