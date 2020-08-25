using EOMobile.Interfaces;
using EOMobile.XamarinFormsCamera;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CameraPage : EOBasePage
    {
        public CameraPage()
        {
            InitializeComponent();

            StartCamera();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        public async void SelectImageClicked(object sender, EventArgs args)
        {
            //StartCamera replaced this
        }

        public async void StartCamera()
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
        private void BackButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }
    }
}