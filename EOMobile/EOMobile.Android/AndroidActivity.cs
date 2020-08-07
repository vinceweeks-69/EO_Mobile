using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using EOMobile.Droid;
using EOMobile.Interfaces;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidActivity))]

namespace EOMobile.Droid
{
    public class AndroidActivity : IAndroidActivity
    {
        public AndroidActivity()
        {

        }
        public void StartActivityInAndroid()
        {
            Intent intent = new Intent();
            intent.AddFlags(ActivityFlags.NewTask);
            intent.AddFlags(ActivityFlags.MultipleTask);
            intent.SetClass(Android.App.Application.Context, typeof(CameraActivity));
            Android.App.Application.Context.StartActivity(intent);
        }
    }

    public class ReturnToMainActivity : IMainActivity
    {
        public ReturnToMainActivity()
        {

        }

        public void SwitchToMainActivity()
        {
            Intent intent = new Intent();
            intent.AddFlags(ActivityFlags.NewTask);
            intent.AddFlags(ActivityFlags.MultipleTask);
            intent.SetClass(Android.App.Application.Context, typeof(MainActivity));
            Android.App.Application.Context.StartActivity(intent);
        }
    }
}