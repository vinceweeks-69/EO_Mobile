using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainPage : ContentPage
	{
		public MainPage ()
		{
			InitializeComponent ();
        }

        public void OnInventoryClicked(object sender, EventArgs e)
        {
            Inventory.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new InventoryPage()).GetAwaiter();
            t.OnCompleted(() => { Inventory.IsEnabled = true; });
        }

        public void OnArrangementsClicked(object sender, EventArgs e)
        {
            Arrangements.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new TabbedArrangementPage()).GetAwaiter();
            t.OnCompleted(() => { Arrangements.IsEnabled = true; });
        }

        public void OnWorkOrdersClicked(object sender, EventArgs e)
        {
            WorkOrders.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new TabbedWorkOrderPage()).GetAwaiter();
            t.OnCompleted(() => { WorkOrders.IsEnabled = true; });
        }

        public void OnCustomersClicked(object sender, EventArgs e)
        {
            Customers.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new CustomerPage()).GetAwaiter();
            t.OnCompleted(() => { Customers.IsEnabled = true; });
        }

        public void OnVendorsClicked(object sender, EventArgs e)
        {
            Vendors.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new VendorPage()).GetAwaiter();
            t.OnCompleted(() => { Vendors.IsEnabled = true; });
        }

        public void OnShipmentsClicked(object sender, EventArgs e)
        {
            Shipments.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new TabbedShipmentPage()).GetAwaiter();
            t.OnCompleted(() => { Shipments.IsEnabled = true; });
        }

        public void OnReportsClicked(object sender, EventArgs e)
        {
            Reports.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new ReportsPage()).GetAwaiter();
            t.OnCompleted(() => { Reports.IsEnabled = true; });
        }

        private void OnSiteServiceClicked(object sender, EventArgs e)
        {
            SiteService.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new TabbedSiteServicePage()).GetAwaiter();
            t.OnCompleted(() => { SiteService.IsEnabled = true; });
        }

        private void Scheduler_Clicked(object sender, EventArgs e)
        {
            Scheduler.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new SchedulerPage()).GetAwaiter();
            t.OnCompleted(() => { Scheduler.IsEnabled = true; });
        }

        private void Bugs_Clicked(object sender, EventArgs e)
        {
            Bugs.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new BugsPage()).GetAwaiter();
            t.OnCompleted(() => { Bugs.IsEnabled = true; });
        }

        private void Help_MainPage_Clicked(object sender, EventArgs e)
        {
            TaskAwaiter t = Navigation.PushAsync(new HelpPage("MainPage")).GetAwaiter();
        }
    }
}