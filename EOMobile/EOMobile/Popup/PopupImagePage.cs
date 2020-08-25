using Android.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;

namespace EOMobile
{
    public partial class PopupImagePage : Rg.Plugins.Popup.Pages.PopupPage
    {
        EOImgData image;

        public ActivityIndicator Spinner { get { return spinner; } }
               
        public PopupImagePage(EOImgData img, string extra = "")
        {
            InitializeComponent();
                       
            if(!String.IsNullOrEmpty(extra))
            {
                extra1.Text = extra;
            }

            if (img != null)
            {
                image = img;

                ImageSource imgSource = ImageSource.FromStream(() => new MemoryStream(image.imgData));

                PopupImage.Source = imgSource;
            }
            else
            {
                this.BackgroundColor = Color.Transparent;
                PopupImage.IsVisible = false;
                spinner.IsEnabled = true;
                spinner.IsRunning = true;
                spinner.IsVisible = true;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        // ### Methods for supporting animations in your popup page ###

        // Invoked before an animation appearing
        protected override void OnAppearingAnimationBegin()
        {
            base.OnAppearingAnimationBegin();
        }

        // Invoked after an animation appearing
        protected override void OnAppearingAnimationEnd()
        {
            base.OnAppearingAnimationEnd();
        }

        // Invoked before an animation disappearing
        protected override void OnDisappearingAnimationBegin()
        {
            base.OnDisappearingAnimationBegin();
        }

        // Invoked after an animation disappearing
        protected override void OnDisappearingAnimationEnd()
        {
            base.OnDisappearingAnimationEnd();
        }

        protected override Task OnAppearingAnimationBeginAsync()
        {
            return base.OnAppearingAnimationBeginAsync();
        }

        protected override Task OnAppearingAnimationEndAsync()
        {
            return base.OnAppearingAnimationEndAsync();
        }

        protected override Task OnDisappearingAnimationBeginAsync()
        {
            return base.OnDisappearingAnimationBeginAsync();
        }

        protected override Task OnDisappearingAnimationEndAsync()
        {
            return base.OnDisappearingAnimationEndAsync();
        }

        // ### Overrided methods which can prevent closing a popup page ###

        // Invoked when a hardware back button is pressed
        protected override bool OnBackButtonPressed()
        {
            if (!spinner.IsRunning)
            {
                // Return true if you don't want to close this popup page when a back button is pressed
                return base.OnBackButtonPressed();
            }
            else
            {
                return true;
            }
        }

        //Invoked when background is clicked
        protected override bool OnBackgroundClicked()
        {

            if (!spinner.IsRunning)
            {
                // Return false if you don't want to close this popup page when a background of the popup page is clicked
                return base.OnBackgroundClicked();
            }
            else
            {
                return false;
            }
        }
    }
}
