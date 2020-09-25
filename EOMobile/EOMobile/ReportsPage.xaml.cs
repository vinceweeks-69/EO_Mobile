using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ReportsPage : EOBasePage
	{
		public ReportsPage ()
		{
			InitializeComponent ();
		}

        public void OnWorkOrderReportClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(TabbedWorkOrderReportPage)))
            {
                Navigation.PushAsync(new TabbedWorkOrderReportPage());
            }
        }
        public void OnShipmentReportClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(TabbedShipmentReportPage)))
            {
                Navigation.PushAsync(new TabbedShipmentReportPage());
            }
        }

        private void OnSiteServiceClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(TabbedSiteServiceReportPage)))
            {
                Navigation.PushAsync(new TabbedSiteServiceReportPage());
            }
        }
    }
}