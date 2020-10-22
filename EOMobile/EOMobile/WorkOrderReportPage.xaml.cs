using EOMobile.ViewModels;
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

            WorkOrderFromDate.Date = DateTime.Today;
            WorkOrderToDate.Date = DateTime.Today;

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
                filter.ToDate = this.WorkOrderToDate.Date.AddHours(23);
                filter.ToDate = filter.ToDate.AddMinutes(59);

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

            ObservableCollection<WorkOrderResponse> list1 = new ObservableCollection<WorkOrderResponse>();

            //for arrangements, add blank rows fore and aft and a header row
            foreach (WorkOrderResponse wo in workOrderList)
            {
                workOrder.Add(wo.WorkOrder);

                inventoryList.Add(wo.WorkOrderList);

                list1.Add(wo);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                WorkOrderList.ItemsSource = list1;
            });
        }

        public void ShowInventory_Clicked(object sender, EventArgs e)
        {
            Button b = sender as Button;

            if (b != null)
            {
                long workOrderId = (long)b.CommandParameter;

                if (workOrderId > 0)
                {
                    WorkOrderResponse r = workOrderList.Where(a => a.WorkOrder.WorkOrderId == workOrderId).First();

                    ObservableCollection<WorkOrderViewModel> list1 = new ObservableCollection<WorkOrderViewModel>();

                    foreach (WorkOrderInventoryMapDTO wor in r.WorkOrderList)
                    {
                        list1.Add(new WorkOrderViewModel(wor));
                    }

                    foreach (NotInInventoryDTO nii in r.NotInInventory)
                    {
                        list1.Add(new WorkOrderViewModel(nii));
                    }

                    foreach (GetArrangementResponse arrangement in r.Arrangements)
                    {
                        list1.Add(new WorkOrderViewModel());    //blank line

                        list1.Add(new WorkOrderViewModel()      //"header"
                        {
                            InventoryName = "Arrangement"
                        });

                        foreach (ArrangementInventoryItemDTO ai in arrangement.ArrangementList)
                        {
                            list1.Add(new WorkOrderViewModel(ai, r.WorkOrder.WorkOrderId));
                        }

                        foreach (NotInInventoryDTO anii in arrangement.NotInInventory)
                        {
                            list1.Add(new WorkOrderViewModel(anii));
                        }

                        list1.Add(new WorkOrderViewModel());  //blank line
                    }

                    InventoryList.ItemsSource = list1;
                }
            }
        }

        public void EditWorkOrder_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(TabbedWorkOrderPage)))
            {
                long workOrderId = (long)(sender as Button).CommandParameter;

                Navigation.PushAsync(new TabbedWorkOrderPage(workOrderId));
            }
        }

        private void WorkOrderCustomer_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(PersonFilterPage)))
            {
                Navigation.PushAsync(new PersonFilterPage(this));
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

        private async void PaymentDetail_Clicked(object sender, EventArgs e)
        {
            Button b = sender as Button;

            if (b != null)
            {
                try
                {
                    WorkOrderResponse workOrder = (WorkOrderResponse)b.CommandParameter;

                    if (workOrder != null)
                    {
                        if (!PageExists(typeof(PaymentPage)))
                        {
                            WorkOrderPaymentDTO payment = await ((App)App.Current).GetWorkOrderPayment(workOrder.WorkOrder.WorkOrderId);

                            if (payment.WorkOrderPaymentId > 0)
                            {
                                Navigation.PushAsync(new PaymentPage(workOrder, payment));
                            }
                            else
                            {
                                DisplayAlert("Unpaid", "This work order has not been paid.", "Ok");
                            }
                        }
                    }
                }
                catch(Exception ex)
                {

                }
            }
        }
    }
}