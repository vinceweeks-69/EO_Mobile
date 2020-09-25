using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainPage : EOBasePage
	{
		public MainPage ()
		{
			InitializeComponent ();

            ((App)App.Current).GetUsers().ContinueWith(a => SetUIForRole(a.Result));
        }

        private void SetUIForRole(GetUserResponse response)
        {
            UserDTO u = response.Users.Where(a => a.UserName == ((App)App.Current).User).FirstOrDefault();

            if (u.UserName == ((App)App.Current).User)
            {
                ((App)App.Current).Role = u.RoleId;
            }

            if (((App)App.Current).Role > 1)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Inventory.IsEnabled = false;
                    Inventory.IsVisible = false;

                    Customers.IsEnabled = false;
                    Customers.IsVisible = false;

                    Vendors.IsEnabled = false;
                    Vendors.IsVisible = false;

                    Reports.IsEnabled = false;
                    Reports.IsVisible = false;

                    Scheduler.IsEnabled = false;
                    Scheduler.IsVisible = false;

                    Shipments.IsEnabled = false;
                    Shipments.IsVisible = false;

                    SiteService.IsEnabled = false;
                    SiteService.IsVisible = false;
                });
            }
        }
        public void OnInventoryClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(InventoryPage)))
            {
                Inventory.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new InventoryPage()).GetAwaiter();
                t.OnCompleted(() => { Inventory.IsEnabled = true; });
            }
        }

        public void OnArrangementsClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(TabbedArrangementPage)))
            {
                Arrangements.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new TabbedArrangementPage()).GetAwaiter();
                t.OnCompleted(() => { Arrangements.IsEnabled = true; });
            }
        }

        public void OnWorkOrdersClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(TabbedWorkOrderPage)))
            {
                WorkOrders.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new TabbedWorkOrderPage()).GetAwaiter();
                t.OnCompleted(() => { WorkOrders.IsEnabled = true; });
            }
        }

        public void OnCustomersClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(CustomerPage)))
            {
                Customers.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new CustomerPage()).GetAwaiter();
                t.OnCompleted(() => { Customers.IsEnabled = true; });
            }
        }

        public void OnVendorsClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(VendorPage)))
            {
                Vendors.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new VendorPage()).GetAwaiter();
                t.OnCompleted(() => { Vendors.IsEnabled = true; });
            }
        }

        public void OnShipmentsClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(TabbedShipmentPage)))
            {
                Shipments.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new TabbedShipmentPage()).GetAwaiter();
                t.OnCompleted(() => { Shipments.IsEnabled = true; });
            }
        }

        public void OnReportsClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(ReportsPage)))
            {
                Reports.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new ReportsPage()).GetAwaiter();
                t.OnCompleted(() => { Reports.IsEnabled = true; });
            }
        }

        private void OnSiteServiceClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(TabbedSiteServicePage)))
            {
                SiteService.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new TabbedSiteServicePage()).GetAwaiter();
                t.OnCompleted(() => { SiteService.IsEnabled = true; });
            }
        }

        private void Scheduler_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(SchedulerPage)))
            {
                Scheduler.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new SchedulerPage()).GetAwaiter();
                t.OnCompleted(() => { Scheduler.IsEnabled = true; });
            }
        }

        private void Bugs_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(BugsPage)))
            {
                Bugs.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new BugsPage()).GetAwaiter();
                t.OnCompleted(() => { Bugs.IsEnabled = true; });
            }
        }

        private void Help_MainPage_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(HelpPage)))
            {
                TaskAwaiter t = Navigation.PushAsync(new HelpPage("MainPage")).GetAwaiter();
            }
        }
    }
}