using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DashboardPage : EOBasePage
    {
        ObservableCollection<WorkOrderResponse> pickupList;
        ObservableCollection<WorkOrderResponse> deliveryList;
        ObservableCollection<WorkOrderResponse> siteServiceList;

        public DashboardPage()
        {
            InitializeComponent();
        }

        void LoadData()
        {
            pickupList = new ObservableCollection<WorkOrderResponse>();
            deliveryList = new ObservableCollection<WorkOrderResponse>();
            siteServiceList = new ObservableCollection<WorkOrderResponse>();

            WorkOrderListFilter workOrderFilter = new WorkOrderListFilter();

            workOrderFilter.FromDate = DateTime.Today;
            workOrderFilter.ToDate = DateTime.Today;
            workOrderFilter.ToDate = workOrderFilter.ToDate.AddHours(23);
            workOrderFilter.Delivery = false;
            workOrderFilter.SiteService = false;

            ((App)App.Current).GetWorkOrders(workOrderFilter).ContinueWith(a => LoadPickups(a.Result)); 
            

            workOrderFilter.Delivery = true;
            ((App)App.Current).GetWorkOrders(workOrderFilter).ContinueWith(a => LoadDeliveries(a.Result));
           

            workOrderFilter.Delivery = false;
            workOrderFilter.SiteService = true;
            ((App)App.Current).GetWorkOrders(workOrderFilter).ContinueWith(a => LoadSiteService(a.Result));
        }

        private void LoadPickups(List<WorkOrderResponse> response)
        {
            pickupList = new ObservableCollection<WorkOrderResponse>();

            foreach(WorkOrderResponse r in response)
            {
                pickupList.Add(r);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                PickupsListView.ItemsSource = pickupList;
            });
        }

        private void LoadDeliveries(List<WorkOrderResponse> response)
        {
            deliveryList = new ObservableCollection<WorkOrderResponse>();

            foreach(WorkOrderResponse r in response)
            {
                deliveryList.Add(r);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                DeliveriesListView.ItemsSource = deliveryList;
            });
        }

        private void LoadSiteService(List<WorkOrderResponse> response)
        {
            siteServiceList = new ObservableCollection<WorkOrderResponse>();

            foreach(WorkOrderResponse r in response)
            {
                siteServiceList.Add(r);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                SiteServiceListView.ItemsSource = siteServiceList;
            });
        }

        protected override void OnAppearing()
        {
            DashboardDate.Text = DateTime.Today.ToLongDateString();

            base.OnAppearing();
            LoadData();
        }

        private void Home_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new MainPage());
        }

        private void WorkOrderDetail_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                //Command parameter is WorkOrderId
                if (button.CommandParameter != null)
                {
                    long workOrderId = (long)button.CommandParameter;
                    Navigation.PushAsync(new TabbedWorkOrderPage(workOrderId));
                }
            }
        }
    }
}