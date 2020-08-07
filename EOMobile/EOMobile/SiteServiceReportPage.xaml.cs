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
    public partial class SiteServiceReportPage : ContentPage
    {
        List<UserDTO> users = new List<UserDTO>();
        List<KeyValuePair<long, string>> employeeDDL = new List<KeyValuePair<long, string>>();
        public List<WorkOrderResponse> workOrderList;
        public List<WorkOrderDTO> workOrder = new List<WorkOrderDTO>();
        public List<List<WorkOrderInventoryMapDTO>> inventoryList = new List<List<WorkOrderInventoryMapDTO>>();
        TabbedSiteServiceReportPage TabParent;
        PersonAndAddressDTO searchedForPerson;
        int searchedForPersonType = 0;
        public SiteServiceReportPage(TabbedSiteServiceReportPage tabParent)
        {
            TabParent = tabParent;
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            users = ((App)App.Current).GetUsers();

            foreach (UserDTO user in users)
            {
                employeeDDL.Add(new KeyValuePair<long, string>(user.UserId, user.UserName));
            }

            ServicedBy.ItemsSource = employeeDDL;

            ServicedBy.SelectedIndex = -1;
        }
        
        private void OnShowSiteServiceReportsClicked(object sender, EventArgs e)
        {
            string message = String.Empty;

            if (SiteServiceFromDate.Date == DateTime.MinValue || SiteServiceToDate.Date == DateTime.MinValue)
            {
                message = "Please enter a 'To' and a 'From' date for this report.";
            }

            if (String.IsNullOrEmpty(message))
            {
                WorkOrderListFilter filter = new WorkOrderListFilter();
                filter.FromDate = this.SiteServiceFromDate.Date;
                filter.ToDate = this.SiteServiceToDate.Date;
                filter.SiteService = true;

                if(ServicedBy.SelectedIndex >= 0)
                {
                    filter.DeliveryUserId = ((KeyValuePair<long, string>)ServicedBy.SelectedItem).Key;
                }

                workOrderList = ((App)App.Current).GetWorkOrders(filter);

                ObservableCollection<WorkOrderDTO> list1 = new ObservableCollection<WorkOrderDTO>();

                foreach (WorkOrderResponse wo in workOrderList)
                {
                    workOrder.Add(wo.WorkOrder);

                    inventoryList.Add(wo.WorkOrderList);

                    list1.Add(wo.WorkOrder);
                }

                SiteServiceList.ItemsSource = list1;
            }
            else
            {
                DisplayAlert("Error", message, "Ok");
            }
        }

        private void ShowInventory_Clicked(object sender, EventArgs e)
        {

        }

        private void EditInventory_Clicked(object sender, EventArgs e)
        {

        }

        private void SiteServiceCustomer_Clicked(object sender, EventArgs e)
        {
            SearchedForPersonType = 0;
            Navigation.PushModalAsync(new PersonFilterPage(this));
        }

        public void EditSiteService_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                long workOrderId = (long)(button.CommandParameter);

                Navigation.PushAsync(new TabbedSiteServicePage(workOrderId));
            }
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

        public int SearchedForPersonType
        {
            get { return searchedForPersonType; }
            set { searchedForPersonType = value; }
        }

        private void Clear_Clicked(object sender, EventArgs e)
        {
            searchedForPerson = null;
            CustomerName.Text = String.Empty;
            SiteServiceFromDate.Date = DateTime.MinValue;
            SiteServiceToDate.Date = DateTime.MinValue;
            ObservableCollection<WorkOrderInventoryMapDTO> list1 = new ObservableCollection<WorkOrderInventoryMapDTO>();
            InventoryList.ItemsSource = list1;
        }
    }
}