using EOMobile.Interfaces;
using Newtonsoft.Json;
using Stripe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WorkOrderPage : ContentPage
	{
        private List<InventoryTypeDTO> inventoryTypeList = new List<InventoryTypeDTO>();
        List<WorkOrderInventoryItemDTO> workOrderInventoryList = new List<WorkOrderInventoryItemDTO>();
        ObservableCollection<KeyValuePair<long, string>> deliveryTypeList = new ObservableCollection<KeyValuePair<long, string>>();
        ObservableCollection<KeyValuePair<long, string>> payTypeList = new ObservableCollection<KeyValuePair<long, string>>();
        ObservableCollection<KeyValuePair<long, string>> serviceTypeList = new ObservableCollection<KeyValuePair<long, string>>();

        List<KeyValuePair<int, string>> discountType = new List<KeyValuePair<int, string>>();

        List<KeyValuePair<int, string>> orderType = new List<KeyValuePair<int, string>>();

        List<KeyValuePair<int, string>> buyerChoiceList = new List<KeyValuePair<int, string>>();

        List<UserDTO> users = new List<UserDTO>();

        List<KeyValuePair<long, string>> employeeDDL = new List<KeyValuePair<long, string>>();

        TabbedWorkOrderPage TabParent = null;

        decimal workOrderTotal = 0;
        long currentWorkOrderId = 0;
        long currentWorkOrderPaymentId = 0;

        long sellerId = 0;
        long customerId = 0;
        long deliveryUserId = 0;
        long deliveryRecipientId = 0;

        PersonAndAddressDTO searchedForPerson = null;
        PersonAndAddressDTO searchedForDeliveryRecipient = null;
        int searchedForPersonType = 0;
        public int SearchedForPersonType
        {
            get { return searchedForPersonType; }
            set { searchedForPersonType = value; }
        }
        public bool EnablePayment()
        {
            bool result = false;
            if(currentWorkOrderId == 0 || (currentWorkOrderId != 0 && currentWorkOrderPaymentId == 0))
            {
                result = true;
            }
            return result;
        }

        public bool EnableSave()
        {
            bool result = false;

            //if it hasn't been paid for, allow an update - check for duplication and only update with "newly added" items
            if (currentWorkOrderId == 0 || currentWorkOrderPaymentId == 0)
            {
                result = true;
            }

            return result;
        }

        private void Initialize(TabbedWorkOrderPage tabParent)
        {
            InitializeComponent();

            TabParent = tabParent;

            MessagingCenter.Subscribe<PersonAndAddressDTO>(this, "SearchCustomer", (arg) =>
            {
                searchedForPerson = arg as PersonAndAddressDTO;
            });

            MessagingCenter.Subscribe<PersonAndAddressDTO>(this, "SearchDeliveryRecipient", (arg) =>
            {
                searchedForDeliveryRecipient = arg as PersonAndAddressDTO;
            });

            MessagingCenter.Subscribe<WorkOrderResponse>(this, "PaymentSuccess", (arg) =>
            {
                OnClear(null,null);
            });

            orderType.Add(new KeyValuePair<int, string>(0, "Walk In"));
            orderType.Add(new KeyValuePair<int, string>(1, "Phone"));
            orderType.Add(new KeyValuePair<int, string>(2, "Email"));

            OrderType.ItemsSource = orderType;
            OrderType.SelectedIndex = 0;

            deliveryTypeList.Add(new KeyValuePair<long, string>(1, "Pickup"));
            deliveryTypeList.Add(new KeyValuePair<long, string>(2, "Delivery"));
            deliveryTypeList.Add(new KeyValuePair<long, string>(3, "Site Service"));

            DeliveryType.ItemsSource = deliveryTypeList;
            DeliveryType.SelectedIndex = 0;

            DeliverTo.Text = "Buyer";

            buyerChoiceList.Add(new KeyValuePair<int, string>(0,"Pick one"));
            buyerChoiceList.Add(new KeyValuePair<int, string>(1,"Choose Existing"));
            buyerChoiceList.Add(new KeyValuePair<int, string>(2, "Create New"));

            BuyerChoice.ItemsSource = buyerChoiceList;
            BuyerChoice.SelectedIndex = 0;

            BuyerChoice.SelectedIndexChanged += BuyerChoice_SelectedIndexChanged;

            users = ((App)App.Current).GetUsers();

            foreach(UserDTO user in users)
            {
                employeeDDL.Add(new KeyValuePair<long, string>(user.UserId, user.UserName));
            }

            DeliveryPerson.ItemsSource = employeeDDL;

            DeliveryPersonLabel.Text = "";
            DeliveryPerson.IsVisible = false;

            DeliverToLabel.Text = "";
            DeliverTo.IsVisible = false;

            DeliveryDateLabel.Text = "Pickup Date";
            DeliveryDate.IsVisible = true;

            Seller.ItemsSource = employeeDDL;
            Seller.SelectedIndex = -1;

            Save.IsEnabled = true;

            //both buttons are disabled until the work order data is saved
            Payment.IsEnabled = false;
            //PaymentType.IsEnabled = false;
        }

        public WorkOrderPage()
        {
            //for XAML Previewer
        }

        public WorkOrderPage (TabbedWorkOrderPage tabParent)
		{
            Initialize(tabParent);
        }

        public WorkOrderPage(TabbedWorkOrderPage tabParent, long workOrderId) 
        {
            Initialize(tabParent);

            //load work order data for the id passed
            WorkOrderResponse workOrder = ((App)App.Current).GetWorkOrder(workOrderId);
            currentWorkOrderId = workOrder.WorkOrder.WorkOrderId;
            
            WorkOrderPaymentDTO workOrderPayment = ((App)App.Current).GetWorkOrderPayment(workOrderId);
            currentWorkOrderPaymentId = workOrderPayment.WorkOrderPaymentId;

            if (currentWorkOrderPaymentId == 0)
            {
                Save.IsEnabled = true;
                Payment.IsEnabled = true;
            }
            else
            {
                if (workOrder.WorkOrder.Paid)
                {
                    InventoryItemsListView.IsEnabled = false;
                    Save.IsEnabled = false;
                    Payment.IsEnabled = false;
                }
                else
                {
                    Save.IsEnabled = true;
                    if (workOrder.WorkOrderList.Count == 0)
                    {
                        Payment.IsEnabled = false;
                    }
                    else
                    {
                        Payment.IsEnabled = true;
                    }
                }
            }

            customerId = workOrder.WorkOrder.CustomerId;

            Buyer.Text = workOrder.WorkOrder.Buyer;

            sellerId = workOrder.WorkOrder.SellerId;
            Seller.SelectedIndex = ((App)App.Current).GetPickerIndex(Seller,workOrder.WorkOrder.SellerId);

            DeliveryType.SelectedIndex = workOrder.WorkOrder.DeliveryType;

            deliveryUserId = workOrder.WorkOrder.DeliveryUserId;
            DeliveryPerson.SelectedIndex = ((App)App.Current).GetPickerIndex(DeliveryPerson,workOrder.WorkOrder.DeliveryUserId);

            DeliveryDate.Date = workOrder.WorkOrder.DeliveryDate;

            deliveryRecipientId = workOrder.WorkOrder.DeliveryRecipientId;

            DeliverTo.Text = workOrder.WorkOrder.DeliverTo;

            WorkOrderDate.Date = workOrder.WorkOrder.CreateDate;

            foreach(var x in workOrder.WorkOrderList)
            {
                workOrderInventoryList.Add(new WorkOrderInventoryItemDTO()
                {
                    WorkOrderId = x.WorkOrderId,
                    InventoryId = x.InventoryId,
                    InventoryName = x.InventoryName,
                    Quantity = x.Quantity,
                    Size = x.Size
                });
            }

            ObservableCollection<WorkOrderInventoryItemDTO> list1 = new ObservableCollection<WorkOrderInventoryItemDTO>(workOrderInventoryList);

            InventoryItemsListView.ItemsSource = list1;

            //load all inventory item images
            //tabParent.LoadWorkOrderImages(workOrderId);
        }

        private void TakePictureClicked(object sender, EventArgs e)
        {
            StartCamera();
        }

        public async void StartCamera()
        {
            var action = await DisplayActionSheet("Add Photo", "Cancel", null, "Choose Existing", "Take Photo");

            if (action == "Choose Existing")
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    var fileName = ((App)App.Current).SetImageFileName();
                    DependencyService.Get<ICameraInterface>().LaunchGallery(FileFormatEnum.JPEG, fileName);
                });
            }
            else if (action == "Take Photo")
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    var fileName = ((App)App.Current).SetImageFileName();
                    DependencyService.Get<ICameraInterface>().LaunchCamera(FileFormatEnum.JPEG, fileName);
                });
            }
        }

        private string SetImageFileName(string fileName = null)
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                if (fileName != null)
                    App.ImageIdToSave = fileName;
                else
                    App.ImageIdToSave = App.EOImageId;

                return App.ImageIdToSave;
            }
            else
            {
                //To iterate, on iOS, if you want to save images to the devie, set 
                if (fileName != null)
                {
                    App.ImageIdToSave = fileName;
                    return fileName;
                }
                else
                    return null;
            }
        }

        protected override void OnDisappearing()
        {

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            WorkOrderInventoryItemDTO searchedForInventory = ((App)App.Current).searchedForInventory;

            if(searchedForInventory != null && searchedForInventory.InventoryId != 0)
            {
                if (!workOrderInventoryList.Contains(searchedForInventory))
                {
                    searchedForInventory.Quantity = 1;

                    if (!workOrderInventoryList.Contains(searchedForInventory))
                    {
                        workOrderInventoryList.Add(searchedForInventory);
                        ObservableCollection<WorkOrderInventoryItemDTO> list1 = new ObservableCollection<WorkOrderInventoryItemDTO>();

                        foreach (WorkOrderInventoryItemDTO wo in workOrderInventoryList)
                        {
                            list1.Add(wo);
                        }

                        InventoryItemsListView.ItemsSource = list1;

                        if (searchedForInventory.ImageId != 0)
                        {
                            EOImgData imageData = ((App)App.Current).GetImage(searchedForInventory.ImageId);

                            if (imageData.imgData != null && imageData.imgData.Length > 0)
                            {
                                TabParent.AddInventoryImage(imageData);
                            }
                        }

                        ((App)App.Current).searchedForInventory = null;
                    }
                }
            }

            GetSearchedPerson();

            GetSearchedDeliveryRecipient();
        }

        void GetSearchedPerson()
        {
            searchedForPerson = ((App)App.Current).searchedForPerson;

            if (searchedForPerson != null && searchedForPerson.Person.person_id != 0)
            {
                Buyer.Text = searchedForPerson.Person.CustomerName;

                ((App)App.Current).searchedForPerson = null;

                customerId = searchedForPerson.Person.person_id;
            }
        }

        void GetSearchedDeliveryRecipient()
        {
            searchedForDeliveryRecipient = ((App)App.Current).searchedForDeliveryRecipient;

            if (searchedForDeliveryRecipient != null && searchedForDeliveryRecipient.Person.person_id != 0)
            {
                DeliverTo.Text = searchedForDeliveryRecipient.Person.CustomerName;

                ((App)App.Current).searchedForDeliveryRecipient = null;

                deliveryRecipientId = searchedForDeliveryRecipient.Person.person_id;
            }
        }

        public void OnDeleteWorkOrderItem(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                long itemId = Int64.Parse(button.CommandParameter.ToString());

                WorkOrderInventoryItemDTO sel = workOrderInventoryList.Where(a => a.InventoryId == itemId).FirstOrDefault();

                if (sel.InventoryId != 0)
                {
                    workOrderInventoryList.Remove(sel);

                    ObservableCollection<WorkOrderInventoryItemDTO> list1 = new ObservableCollection<WorkOrderInventoryItemDTO>();

                    foreach (WorkOrderInventoryItemDTO wo in workOrderInventoryList)
                    {
                        list1.Add(wo);
                    }

                    InventoryItemsListView.ItemsSource = list1;
                }
            }
        }

        public void OnInventorySearchClicked(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new ArrangementFilterPage(this));
        }

        public void OnCustomerSearchClicked(object sender, EventArgs e)
        {
            SearchedForPersonType = 0;
            Navigation.PushModalAsync(new PersonFilterPage(this));
        }

        private string ValidateSaveWorkOrder()
        {
            string errorMessage = String.Empty;

            if(workOrderInventoryList.Count == 0)
            {
                errorMessage += "Please add at least one inventory item \n";
            }

            if(Seller.SelectedIndex < 0)
            {
                errorMessage += "Please add the seller's name \n";
            }

            if (String.IsNullOrEmpty(Buyer.Text))
            {
                errorMessage += "Please add the buyer's name \n";
            }

            return errorMessage;
        }

        public void OnSaveWorkOrder(object sender, EventArgs e)
        {
            string errorMessage = ValidateSaveWorkOrder();

            if(String.IsNullOrEmpty(errorMessage))
            {
                AddWorkOrder();
            }
            else
            {
                DisplayAlert("Can't save work order", errorMessage, "Ok");
            }
        }

        public void OnClear(object sender, EventArgs e)
        {
            currentWorkOrderId = 0;
            currentWorkOrderPaymentId = 0;
            searchedForPerson = null;

            sellerId = 0;
            customerId = 0;
            deliveryUserId = 0;
            deliveryRecipientId = 0;

            Buyer.Text = String.Empty;
            WorkOrderDate.Date = DateTime.Now;

            Seller.SelectedIndex = 1;

            DeliverTo.Text = "";

            DeliveryType.SelectedIndex = 0;
            //ServiceType.SelectedIndex = 0;

            this.workOrderInventoryList.Clear();
            this.InventoryItemsListView.ItemsSource = null;
        }

        public void AddWorkOrder()
        {
            AddWorkOrderRequest addWorkOrderRequest = new AddWorkOrderRequest();

            WorkOrderDTO dto = new WorkOrderDTO()
            {
                WorkOrderId = currentWorkOrderId,
                Seller = ((KeyValuePair<long, string>)this.Seller.SelectedItem).Value,
                Buyer = this.Buyer.Text,
                CreateDate = DateTime.Now,
                DeliveryDate = this.DeliveryDate.Date,
                Comments = this.Comments.Text,
                //IsSiteService = this.ServiceType.SelectedIndex == 0 ? false : true,
                IsSiteService = this.DeliveryType.SelectedIndex == 2 ? true : false, 
                IsDelivery = this.DeliveryType.SelectedIndex == 0 ? false : true,
                DeliveryType = this.DeliveryType.SelectedIndex,
                Paid = false,
                IsCancelled = false,
                CustomerId = customerId,
                SellerId = ((KeyValuePair<long,string>)this.Seller.SelectedItem).Key, 
                DeliverTo = DeliverTo.Text,
                DeliveryRecipientId = deliveryRecipientId,
                OrderType = OrderType.SelectedIndex,
                DeliveryUserId = this.DeliveryPerson.SelectedItem != null ? ((KeyValuePair<long, string>)this.DeliveryPerson.SelectedItem).Key : 0
            };

            List<WorkOrderInventoryMapDTO> workOrderInventoryMap = new List<WorkOrderInventoryMapDTO>();

            foreach (WorkOrderInventoryItemDTO woii in workOrderInventoryList)
            {
                workOrderInventoryMap.Add(new WorkOrderInventoryMapDTO()
                {
                    InventoryId = woii.InventoryId,
                    InventoryName = woii.InventoryName,
                    Quantity = woii.Quantity
                });
            }

            addWorkOrderRequest.WorkOrder = dto;
            addWorkOrderRequest.WorkOrderInventoryMap = workOrderInventoryMap;

            currentWorkOrderId = ((App)App.Current).AddWorkOrder(addWorkOrderRequest);

            if(currentWorkOrderId > 0)
            {
                //add any images
                List<EOImgData> imageData = ((App)App.Current).GetImageData();

                if(imageData.Count > 0)
                {
                    foreach (EOImgData img in imageData)
                    {
                        AddWorkOrderImageRequest request = new AddWorkOrderImageRequest()
                        {
                            WorkOrderId = currentWorkOrderId,
                            ImageId = img.ImageId,
                            Image = img.imgData
                        };

                        ((App)App.Current).AddWorkOrderImage(request);
                    }
                }

                ((App)App.Current).ClearImageData();

                Save.IsEnabled = false;
                //PaymentType.IsEnabled = true;
                Payment.IsEnabled = true;
            }
            else
            {
                DisplayAlert("Error", "There was an error saving this work order.", "Ok");
            }
        }

        private void Quantity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            int debug = 1;
        }
        private void Payment_Clicked(object sender, EventArgs e)
        {
            if (currentWorkOrderId > 0)
            {
                Navigation.PushModalAsync(new PaymentPage(currentWorkOrderId, workOrderInventoryList));
            }
        }

        private void DeliveryType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Picker p = sender as Picker;

            if(p != null)
            {
                if(p.SelectedIndex == 0)  //Pickup
                {
                    DeliveryPersonLabel.Text = "";
                    DeliveryPerson.IsVisible = false;

                    DeliverToLabel.Text = "";
                    DeliverTo.IsVisible = false;

                    DeliveryDateLabel.Text = "Pickup Date";
                    DeliveryDate.IsVisible = true;
                }
                else
                {
                    DeliveryPersonLabel.Text = "Delivery Person";
                    DeliveryPerson.IsVisible = true;

                    DeliverToLabel.Text = "Deliver To";
                    DeliverTo.IsVisible = true;

                    DeliveryDateLabel.Text = "Delivery Date";
                    DeliveryDate.IsVisible = true;
                }

                //if(p.SelectedIndex == 1)
                //{
                //    DeliveryPersonLabel.Text = "Delivery Person";
                //    DeliveryPerson.IsVisible = true;

                //    DeliverToLabel.Text = "Deliver To";
                //    DeliverTo.IsVisible = true;

                //    DeliveryDateLabel.Text = "Delivery Date";
                //    DeliveryDate.IsVisible = true;
                //}
                //else
                //{
                //    DeliveryPersonLabel.Text = "";
                //    DeliveryPerson.IsVisible = false;

                //    DeliverToLabel.Text = "";
                //    DeliverTo.IsVisible = false;

                //    DeliveryDateLabel.Text = "";
                //    DeliveryDate.IsVisible = false;
                //}
            }
        }

        private void Entry_Focused(object sender, FocusEventArgs e)
        {
            //SearchedForPersonType = 1;
            //Navigation.PushModalAsync(new PersonFilterPage(this));
        }

        private void AddInventory_Clicked(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new ArrangementFilterPage(this));
        }

        private void Buyer_Focused(object sender, FocusEventArgs e)
        {
            SearchedForPersonType = 0;
            Navigation.PushModalAsync(new PersonFilterPage(this));
        }

        private void OrderType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void BuyerChoice_SelectedIndexChanged(object sender, EventArgs e)
        {
            //either show Person Filter or Create New Customer
            Picker p = sender as Picker;

            int debug = 0;

            if (p != null)
            {
                SearchedForPersonType = 0;

                if (p.SelectedIndex == 1)
                {
                    Navigation.PushModalAsync(new PersonFilterPage(this));
                }
                else if(p.SelectedIndex == 2)
                {
                    Navigation.PushModalAsync(new CustomerPage(this));
                }
            }
        }
    }
}