using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using EOMobile.Interfaces;


[assembly: Xamarin.Forms.Dependency(typeof(EOMobile.Droid.WifiActivity))]

namespace EOMobile.Droid
{
    public class WifiActivity : IWifiActivity
    {
        public WifiActivity()
        {

        }

        public string GetHostName()
        {
            string hostName = String.Empty;

            WifiManager wifiManager = (WifiManager)Application.Context.GetSystemService(Context.WifiService);

            if (wifiManager != null)
            {
                hostName = wifiManager.ConnectionInfo.SSID;
            }
            else
            {
                hostName = "WiFiManager is NULL";
            }

            return hostName;
        }
    }
}