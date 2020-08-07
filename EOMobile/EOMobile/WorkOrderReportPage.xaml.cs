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
    public partial class WorkOrderReportPage : ContentPage
    {
        public List<WorkOrderResponse> workOrderList;
        public List<WorkOrderDTO> workOrder = new List<WorkOrderDTO>();
        public List<List<WorkOrderInventoryMapDTO>> inventoryList = new List<List<WorkOrderInventoryMapDTO>>();

        ObservableCollection<KeyValuePair<long, string>> deliveryTypeList = new ObservableCollection<KeyValuePair<long, string>>();
        ObservableCollection<KeyValuePair<long, string>> paidList = new ObservableCollection<KeyValuePair<long, string>>();
        ObservableCollection<KeyValuePair<long, string>> siteServiceList = new ObservableCollection<KeyValuePair<long, string>>();

        public TabbedWorkOrderReportPage TabParent;

        PersonAndAddressDTO searchedForPerson;

        List<UserDTO> users = new List<UserDTO>();

        List<KeyValuePair<long, string>> employeeDDL = new List<KeyValuePair<long, string>>();

        public WorkOrderReportPage(TabbedWorkOrderReportPage tabParent)
        {
            InitializeComponent();
            TabParent = tabParent;

            paidList.Add(new KeyValuePair<long, string>(1, "No"));
            paidList.Add(new KeyValuePair<long, string>(2, "Yes"));

            WorkOrderPaid.ItemsSource = paidList;
            WorkOrderPaid.SelectedIndex = 0;

            deliveryTypeList.Add(new KeyValuePair<long, string>(1, "Pickup"));
            deliveryTypeList.Add(new KeyValuePair<long, string>(2, "Delivery"));

            WorkOrderDelivery.ItemsSource = deliveryTypeList;
            WorkOrderDelivery.SelectedIndex = 0;

            siteServiceList.Add(new KeyValuePair<long, string>(1, "No"));
            siteServiceList.Add(new KeyValuePair<long, string>(2, "Yes"));

            WorkOrderSiteService.ItemsSource = siteServiceList;
            WorkOrderSiteService.SelectedIndex = 0;

            users = ((App)App.Current).GetUsers();

            foreach (UserDTO user in users)
            {
                employeeDDL.Add(new KeyValuePair<long, string>(user.UserId, user.UserName));
            }

            DeliveredBy.ItemsSource = employeeDDL;

        }

        public void OnShowReportsClicked(object sender, EventArgs e)
        {
            string message = String.Empty;

            if (WorkOrderFromDate.Date == DateTime.MinValue || WorkOrderToDate.Date == DateTime.MinValue)
            {
                message = "Please enter a 'To' and a 'From' date for this report.";
            }

            //check to make sure the report from and to dates are not too far apart

            if (String.IsNullOrEmpty(message))
            {
                WorkOrderListFilter filter = new WorkOrderListFilter();
                filter.FromDate = this.WorkOrderFromDate.Date;
                filter.ToDate = this.WorkOrderToDate.Date;

                workOrderList = ((App)App.Current).GetWorkOrders(filter);

                ObservableCollection<WorkOrderDTO> list1 = new ObservableCollection<WorkOrderDTO>();

                foreach (WorkOrderResponse wo in workOrderList)
                {
                    workOrder.Add(wo.WorkOrder);

                    inventoryList.Add(wo.WorkOrderList);

                    list1.Add(wo.WorkOrder);
                }

                WorkOrderList.ItemsSource = list1;
            }
            else
            {
                DisplayAlert("Error", message, "Ok");
            }
        }

        public void ShowInventory_Clicked(object sender, EventArgs e)
        {
            //the  user has clicked on a line item work order record from the top list
            //get the list of inventory items for the work order that was clicked on

            long workOrderId = (long)(sender as Button).CommandParameter;

            List<WorkOrderInventoryMapDTO> inventory = workOrderList.Where(a => a.WorkOrder.WorkOrderId == workOrderId).Select(b => b.WorkOrderList).First();

            ObservableCollection<WorkOrderInventoryMapDTO> list1 = new ObservableCollection<WorkOrderInventoryMapDTO>();

            foreach (WorkOrderInventoryMapDTO i in inventory)
            {
                list1.Add(i);
            }

            InventoryList.ItemsSource = list1;
        }

        public void EditWorkOrder_Clicked(object sender, EventArgs e)
        {
            long workOrderId = (long)(sender as Button).CommandParameter;

            Navigation.PushAsync(new TabbedWorkOrderPage(workOrderId));
        }

        private void WorkOrderCustomer_Clicked(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new PersonFilterPage(this));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            searchedForPerson = ((App)App.Current).searchedForPerson;

            if (searchedForPerson != null && searchedForPerson.Person.person_id != 0)
            {
                CustomerName.Text = searchedForPerson.Person.CustomerName;

                ((App)App.Current).searchedForPerson = null;
            }
        }

        private void Clear_Clicked(object sender, EventArgs e)
        {
            searchedForPerson = null;
            CustomerName.Text = String.Empty;
            WorkOrderFromDate.Date = DateTime.MinValue;
            WorkOrderToDate.Date = DateTime.MinValue;
            DeliveredBy.SelectedIndex = -1;
            WorkOrderPaid.SelectedIndex = -1;
            WorkOrderSiteService.SelectedIndex = -1;
            WorkOrderDelivery.SelectedIndex = -1;

            ObservableCollection<WorkOrderInventoryMapDTO> list1 = new ObservableCollection<WorkOrderInventoryMapDTO>();
            InventoryList.ItemsSource = list1;
        }
    }
}