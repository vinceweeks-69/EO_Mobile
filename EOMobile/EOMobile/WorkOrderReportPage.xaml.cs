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
    public partial class WorkOrderReportPage : EOBasePage
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

            ((App)App.Current).GetUsers().ContinueWith(a => LoadUserData(a.Result));
        }

        private void LoadUserData(GetUserResponse userResponse)
        {
            if (TabParent.CustomerId == 0)
            {
                users = ((App)App.Current).GetUsers().Result.Users;
            }
            else
            {
                users.Add(((App)App.Current).GetUsers().Result.Users.Where(a => a.UserId == TabParent.CustomerId).FirstOrDefault());
            }

            foreach (UserDTO user in users)
            {
                employeeDDL.Add(new KeyValuePair<long, string>(user.UserId, user.UserName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                DeliveredBy.ItemsSource = employeeDDL;
            });
        }

        public void OnShowReportsClicked(object sender, EventArgs e)
        {
            string message = String.Empty;

            if(TabParent.CustomerId != 0)
            {
                WorkOrderFromDate.Date = DateTime.Now.AddMonths(-6);
                WorkOrderToDate.Date = DateTime.Now;
            }

            if (WorkOrderFromDate.Date == DateTime.MinValue || WorkOrderToDate.Date == DateTime.MinValue)
            {
                message = "Please enter a 'To' and a 'From' date for this report.";
            }

            //check to make sure the report from and to dates are not too far apart

            if (String.IsNullOrEmpty(message))
            {
                WorkOrderListFilter filter = new WorkOrderListFilter();

                if(TabParent.CustomerId != 0)
                {
                    filter.CustomerId = TabParent.CustomerId;
                }

                filter.FromDate = this.WorkOrderFromDate.Date;
                filter.ToDate = this.WorkOrderToDate.Date;

                ((App)App.Current).GetWorkOrders(filter).ContinueWith(a => WorkOrdersLoaded(a.Result));
            }
            else
            {
                DisplayAlert("Error", message, "Ok");
            }
        }

        private void WorkOrdersLoaded(List<WorkOrderResponse> response)
        {
            workOrderList = response;

            ObservableCollection<WorkOrderDTO> list1 = new ObservableCollection<WorkOrderDTO>();

            foreach (WorkOrderResponse wo in workOrderList)
            {
                workOrder.Add(wo.WorkOrder);

                inventoryList.Add(wo.WorkOrderList);

                list1.Add(wo.WorkOrder);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                WorkOrderList.ItemsSource = list1;
            });
        }

        public void ShowInventory_Clicked(object sender, EventArgs e)
        {
            //the  user has clicked on a line item work order record from the top list
            //get the list of inventory items for the work order that was clicked on

            long workOrderId = (long)(sender as Button).CommandParameter;

            List<WorkOrderInventoryMapDTO> inventory = workOrderList.Where(a => a.WorkOrder.WorkOrderId == workOrderId).Select(b => b.WorkOrderList).First();

            ObservableCollection<WorkOrderInventoryMapDTO> list1 = new ObservableCollection<WorkOrderInventoryMapDTO>();

            //if there are arrangements that are part of this work order, group the inventory items together add blank lines start and
            //end and add a "header" row so Melissa will be happy

            Dictionary<long, List<WorkOrderInventoryMapDTO>> arrangements = new Dictionary<long, List<WorkOrderInventoryMapDTO>>();
            
            foreach (WorkOrderInventoryMapDTO i in inventory)
            {
                if (i.GroupId.HasValue)
                {
                    if (arrangements.Keys.Contains(i.GroupId.Value))
                    {
                        arrangements[i.GroupId.Value].Add(i);
                    }
                    else
                    {
                        List<WorkOrderInventoryMapDTO> inventoryItems = new List<WorkOrderInventoryMapDTO>();
                        inventoryItems.Add(i);
                        arrangements.Add(i.GroupId.Value, inventoryItems);
                    }
                }
                else
                {
                    list1.Add(i);
                }
            }

            if(arrangements.Count > 0)
            {
                foreach(KeyValuePair<long,List<WorkOrderInventoryMapDTO>> kvp in arrangements)
                {
                    //add a blank line 
                    list1.Add(new WorkOrderInventoryMapDTO()
                    {
                        GroupId = kvp.Key
                    });

                    //add a "header" line
                    list1.Add(new WorkOrderInventoryMapDTO()
                    {
                        InventoryName = "Arrangement",
                        GroupId = kvp.Key
                    }); 

                    //add inventory items
                    foreach(WorkOrderInventoryMapDTO a in kvp.Value)
                    {
                        list1.Add(a);
                    }

                    //add a blank line
                    list1.Add(new WorkOrderInventoryMapDTO()
                    {
                        GroupId = kvp.Key
                    });
                }
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
            Navigation.PushAsync(new PersonFilterPage(this));
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