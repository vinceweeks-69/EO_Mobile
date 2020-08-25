using SharedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BugsPage : EOBasePage
    {
        public BugsPage()
        {
            InitializeComponent();
        }

        private void sendBugReport_Clicked(object sender, EventArgs e)
        {
            if(bugReport.Text.Length > 0)
            {
                string msg = ((App)App.Current).User + "says: " + bugReport.Text;

                EOMailMessage mailMessage = new EOMailMessage("service@elegantorchids.com", "vinceweeks@yahoo.com", "User Report", msg, "Orchids@5185");

                if (mailMessage.MailMessage != null)
                {
                    Email.SendEmail(mailMessage);

                    DisplayAlert("Email Report", "Your message has been set to the development team", "OK");
                }

                bugReport.Text = String.Empty;
            }
        }
    }
}