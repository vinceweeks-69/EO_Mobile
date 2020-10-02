using Android.Content;

namespace EOMobile.Droid
{
    public class NetworkBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            WifiActivity wifiActivity = new WifiActivity();

            string hostName = wifiActivity.GetHostName();

            ((App)App.Current).ChangeLANAddress(hostName);
        }
    }
}