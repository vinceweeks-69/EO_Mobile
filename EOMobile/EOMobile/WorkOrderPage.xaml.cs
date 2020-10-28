using EOMobile.Converters;
using EOMobile.Interfaces;
using EOMobile.ViewModels;
using Newtonsoft.Json;
using SharedData;
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
	public partial class WorkOrderPage : EOBasePage
	{
        //if a work order contains an arrangement, save that data here
        List<AddArrangementRequest> arrangementList = new List<AddArrangementRequest>();
        List<WorkOrderInventoryItemDTO> workOrderInventoryList = new List<WorkOrderInventoryItemDTO>();
        List<NotInInventoryDTO> notInInventory = new List<NotInInventoryDTO>();

        List<GetArrangementResponse> arrangements = new List<GetArrangementResponse>();

        ObservableCollection<WorkOrderViewModel> workOrderList = new ObservableCollection<WorkOrderViewModel>();

        List<InventoryTypeDTO> inventoryTypeList = new List<InventoryTypeDTO>();
       
        ObservableCollection<KeyValuePair<long, string>> deliveryTypeList = new ObservableCollection<KeyValuePair<long, string>>();
        ObservableCollection<KeyValuePair<long, string>> payTypeList = new ObservableCollection<KeyValuePair<long, string>>();
        ObservableCollection<KeyValuePair<long, string>> serviceTypeList = new ObservableCollection<KeyValuePair<long, string>>();

        List<KeyValuePair<int, string>> discountType = new List<KeyValuePair<int, string>>();

        List<KeyValuePair<int, string>> orderType = new List<KeyValuePair<int, string>>();

        List<KeyValuePair<int, string>> buyerChoiceList = new List<KeyValuePair<int, string>>();

        List<UserDTO> users = new List<UserDTO>();

        //List<KeyValuePair<long, string>> employeeDDL = new List<KeyValuePair<long, string>>();

        TabbedWorkOrderPage TabParent = null;

        long currentWorkOrderId = 0;
        long currentWorkOrderPaymentId = 0;

        long sellerId = 0;

        long customerId = 0;
        //PersonAndAddressDTO workOrderCustomer = new PersonAndAddressDTO();

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

        public PersonAndAddressDTO Customer
        {
            get { return searchedForPerson; }
        }

        WorkOrderInventoryItemDTO searchedForInventory;

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

            MessagingCenter.Subscribe<WorkOrderInventoryItemDTO>(this, "SearchInventory", (arg) =>
            {
                searchedForInventory = arg;
            });

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

            ((App)App.Current).GetUsers().ContinueWith(a => LoadUsers(a.Result));

            Save.IsEnabled = true;

            Save.Clicked += OnSaveWorkOrder;

            //both buttons are disabled until the work order data is saved
            Payment.IsEnabled = false;
        }

        private void LoadUsers(GetUserResponse userResponse)
        {
            users = userResponse.Users;

            ObservableCollection<KeyValuePair<long, string>> deliverers = new ObservableCollection<KeyValuePair<long, string>>();
            ObservableCollection<KeyValuePair<long, string>> notDeliverers = new ObservableCollection<KeyValuePair<long, string>>();

            foreach (UserDTO user in users)
            {
                if (user.RoleId != 2)
                {
                    deliverers.Add(new KeyValuePair<long, string>(user.UserId, user.UserName));
                }

                if(user.RoleId < 3)
                {
                    notDeliverers.Add(new KeyValuePair<long, string>(user.UserId, user.UserName));
                }
            }


            Device.BeginInvokeOnMainThread(() =>
            {
                DeliveryPerson.ItemsSource = deliverers;

                DeliveryPersonLabel.Text = "";
                DeliveryPerson.IsVisible = false;

                DeliverToLabel.Text = "";
                DeliverTo.IsVisible = false;

                DeliveryDateLabel.Text = "Pickup Date";
                DeliveryDate.IsVisible = true;

                Seller.ItemsSource = notDeliverers;
                Seller.SelectedIndex = -1;
            });
        }

        public WorkOrderPage()
        {
            //for XAML Previewer
        }

        public WorkOrderPage (TabbedWorkOrderPage tabParent)
		{
            Initialize(tabParent);
        }

        /// <summary>
        /// Special handling for arrangements so that they display nicely
        /// </summary>
        /// <param name="tabParent"></param>
        /// <param name="workOrderId"></param>
        public WorkOrderPage(TabbedWorkOrderPage tabParent, long workOrderId) 
        {
            Initialize(tabParent);

            //load work order data for the id passed
            ((App)App.Current).GetWorkOrder(workOrderId).ContinueWith(a => WorkOrderLoaded(a.Result));
            
            //load all inventory item images
            //tabParent.LoadWorkOrderImages(workOrderId);
        }

        private void WorkOrderLoaded(WorkOrderResponse workOrderResponse)
        {
            currentWorkOrderId = workOrderResponse.WorkOrder.WorkOrderId;
            ((App)App.Current).GetWorkOrderPayment(currentWorkOrderId).ContinueWith(a => WorkOrderPaymentLoaded(workOrderResponse, a.Result));
        }

        //Load and possibly convert data into relevant data lists
        private void WorkOrderPaymentLoaded(WorkOrderResponse workOrder, WorkOrderPaymentDTO payment)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                currentWorkOrderId = workOrder.WorkOrder.WorkOrderId;

                searchedForPerson = new PersonAndAddressDTO();
                searchedForPerson.Person.person_id = workOrder.WorkOrder.CustomerId;
                customerId = searchedForPerson.Person.person_id;

                currentWorkOrderPaymentId = payment.WorkOrderPaymentId;

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
                        AddInventory.IsEnabled = false;
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

                Buyer.Text = workOrder.WorkOrder.Buyer;

                sellerId = workOrder.WorkOrder.SellerId;
                Seller.SelectedIndex = ((App)App.Current).GetPickerIndex(Seller, workOrder.WorkOrder.SellerId);

                DeliveryType.SelectedIndex = workOrder.WorkOrder.DeliveryType;

                deliveryUserId = workOrder.WorkOrder.DeliveryUserId;
                DeliveryPerson.SelectedIndex = ((App)App.Current).GetPickerIndex(DeliveryPerson, workOrder.WorkOrder.DeliveryUserId);

                DeliveryDate.Date = workOrder.WorkOrder.DeliveryDate;

                deliveryRecipientId = workOrder.WorkOrder.DeliveryRecipientId;

                DeliverTo.Text = workOrder.WorkOrder.DeliverTo;

                WorkOrderDate.Date = workOrder.WorkOrder.CreateDate;

                workOrderList.Clear();

                notInInventory = workOrder.NotInInventory;

                //convert between duplicate types - refactor
                foreach (var x in workOrder.WorkOrderList)
                {
                    WorkOrderInventoryItemDTO dto =
                        new WorkOrderInventoryItemDTO()
                        {
                            WorkOrderId = x.WorkOrderId,
                            InventoryId = x.InventoryId,
                            InventoryName = x.InventoryName,
                            Quantity = x.Quantity,
                            Size = x.Size,
                            GroupId = x.GroupId
                        };

                    workOrderInventoryList.Add(dto);
                }

                foreach (GetArrangementResponse ar in workOrder.Arrangements)
                {
                    AddArrangementRequest aaReq = new AddArrangementRequest();

                    aaReq.Arrangement = ar.Arrangement;
                    aaReq.Inventory = ar.Inventory;
                    aaReq.ArrangementInventory = ar.ArrangementList;
                    aaReq.GroupId = ar.Arrangement.ArrangementId;
                    aaReq.NotInInventory = ar.NotInInventory;
                    
                    arrangementList.Add(aaReq);
                }

                RedrawInventoryList();
            });
        }

        //modify the underlying data lists, then call this function
        private void RedrawInventoryList()
        {
            workOrderList.Clear();

            //draw work order items in inventory
            foreach(WorkOrderInventoryItemDTO dto in workOrderInventoryList)
            {
                WorkOrderViewModel vm = new WorkOrderViewModel(dto);
                vm.ShouldEnable = currentWorkOrderPaymentId > 0 ? false : true;
                workOrderList.Add(vm);
            }

            //draw work order item Not in Inventory
            foreach(NotInInventoryDTO dto in notInInventory)
            {
                WorkOrderViewModel vm = new WorkOrderViewModel(dto);
                vm.ShouldEnable = currentWorkOrderPaymentId > 0 ? false : true;
                workOrderList.Add(vm);
            }

            //draw arangements with header row and blank rows top and bottom
            foreach (AddArrangementRequest ar in arrangementList)
            {
                //add a blank row
                workOrderList.Add(new WorkOrderViewModel()
                {
                    WorkOrderId = currentWorkOrderId,
                    InventoryId = 0,
                    InventoryName = String.Empty,
                    Quantity = 0,
                    Size = String.Empty,
                    GroupId = ar.ArrangementInventory[0].ArrangementId
                });

                //add a Header row
                workOrderList.Add(new WorkOrderViewModel()
                {
                    WorkOrderId = currentWorkOrderId,
                    InventoryId = 0,
                    InventoryName = "Arrangement",
                    Quantity = 0,
                    Size = String.Empty,
                    GroupId = ar.ArrangementInventory[0].ArrangementId
                });

                ar.ArrangementInventory.Where(a => a.InventoryId != 0).ToList().ForEach(aid =>
                {
                    workOrderList.Add(new WorkOrderViewModel()
                    {
                        WorkOrderId = currentWorkOrderId,
                        InventoryId = aid.InventoryId,
                        InventoryTypeId = aid.InventoryTypeId,
                        InventoryName = aid.InventoryName,
                        Quantity = aid.Quantity,
                        Size = aid.Size,
                        GroupId = aid.ArrangementId,
                        ShouldEnable = currentWorkOrderPaymentId > 0 ? false : true
                    });
                });

                ar.NotInInventory.Where(b => b.ArrangementId != null && b.ArrangementId != 0).ToList().ForEach(x =>
                {

                    WorkOrderInventoryItemDTO dto =
                        new WorkOrderInventoryItemDTO()
                        {
                            WorkOrderId = x.WorkOrderId,
                            InventoryId = 0,
                            InventoryName = x.NotInInventoryName,
                            Size = x.NotInInventorySize,
                            NotInInventoryName = x.NotInInventoryName,
                            Quantity = x.NotInInventoryQuantity,
                            NotInInventorySize = x.NotInInventorySize,
                            NotInInventoryPrice = x.NotInInventoryPrice,
                            GroupId = x.ArrangementId,
                        };

                    WorkOrderViewModel vm = new WorkOrderViewModel(dto);
                    vm.ShouldEnable = currentWorkOrderPaymentId > 0 ? false : true;
                    workOrderList.Add(vm);

                });

                //add a blank row
                workOrderList.Add(new WorkOrderViewModel()
                {
                    WorkOrderId = currentWorkOrderId,
                    InventoryId = 0,
                    InventoryName = String.Empty,
                    Quantity = 0,
                    Size = String.Empty,
                    GroupId = ar.ArrangementInventory[0].ArrangementId
                });
            }

            InventoryItemsListView.ItemsSource = workOrderList;
        }

        private void TakePictureClicked(object sender, EventArgs e)
        {
            StartCamera();
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
                //To iterate, on iOS, if you want to save images to the device, set 
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

            GetSearchedArrangement();

            GetSearchedForNotInInventory();

            RedrawInventoryList();
        }

        void GetSearchedPerson()
        {
            if (((App)App.Current).searchedForPerson != null)
            {
                searchedForPerson = new PersonAndAddressDTO();
                customerId = ((App)App.Current).searchedForPerson.Person.person_id;
                searchedForPerson.Person.person_id = ((App)App.Current).searchedForPerson.Person.person_id;
                searchedForPerson.Person.first_name = ((App)App.Current).searchedForPerson.Person.first_name;
                searchedForPerson.Person.last_name = ((App)App.Current).searchedForPerson.Person.last_name;

                Buyer.Text = searchedForPerson.Person.CustomerName;
                ((App)App.Current).searchedForPerson = null;
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

        /// <summary>
        /// Called when arrangement data is "added" to a work order - need to use groupId variable in arrangement DTOs if PKs don't yet exist
        /// This function is always called in OnAppearing which calls as it's last job the Redraw function
        /// </summary>
        void GetSearchedArrangement()
        {
            if(((App)App.Current).searchedForArrangement != null)
            {
                AddArrangementRequest aar = ((App)App.Current).searchedForArrangement;

                //when an arrangement is being created "on the fly" for a work order,
                //we need a way to group all the arrangement parts so that we can go back and forth
                // between the work order page and the arrangement page, if we need to.

                long? groupId = null;
                var rand = new Random();

                if (aar.Arrangement.ArrangementId == 0)
                {
                    if (!aar.GroupId.HasValue)
                    {
                        groupId = rand.Next(255);
                        aar.GroupId = groupId;
                    }
                    else
                    {
                        groupId = aar.GroupId;
                    }
                }
                else
                {
                    groupId = aar.Arrangement.ArrangementId;
                }

                if(aar.Arrangement.ArrangementId == 0)
                {
                    arrangementList.Add(aar);
                }
                else
                {
                    if(arrangementList.Where(a => a.Arrangement.ArrangementId == aar.Arrangement.ArrangementId).Any())
                    {
                        AddArrangementRequest temp2 = arrangementList.Where(a => a.Arrangement.ArrangementId == aar.Arrangement.ArrangementId).First();
                        arrangementList.Remove(temp2);
                        arrangementList.Add(aar);
                    }
                }
  
                ((App)App.Current).searchedForArrangement = null;
            }
        }

        void GetSearchedForNotInInventory()
        {
            if(((App)App.Current).notInInventory_toAdd != null && !NotInInventoryItemIsinList(((App)App.Current).notInInventory_toAdd))
            {
                notInInventory.Add(((App)App.Current).notInInventory_toAdd);
                ((App)App.Current).notInInventory_toAdd = null;
            }
        }

        //the arrangement data is kept on this page - so the management for 
        //both work order inventory and not in inventory lists as well as the
        //arrangement inventory and arrangement not in inventory lists is done here
        //if deletion happens here - if an item is deleted on the arrangement page,
        //then only arrangement data is modified, if a modification is made on the
        //arrangement page AND Save is pressed, the modified data is synced with
        //the data on this page
        public void OnDeleteWorkOrderItem(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                WorkOrderViewModel sel = button.CommandParameter as WorkOrderViewModel;

                if (sel != null)
                {
                    if(sel.InventoryId == 0)  //"Not in inventory"
                    {
                        if(sel.GroupId == 0)   //not in arrangement
                        {
                            if (notInInventory.Where(a => a.NotInInventoryName == sel.InventoryName && a.NotInInventorySize == sel.Size &&
                                 a.NotInInventoryQuantity == sel.Quantity).Any())
                            {
                                NotInInventoryDTO dto = notInInventory.Where(a => a.NotInInventoryName == sel.InventoryName && a.NotInInventorySize == sel.Size &&
                                a.NotInInventoryQuantity == sel.Quantity).First();

                                notInInventory.Remove(dto);
                            }
                        }
                        else
                        {
                            RemoveItemFromArrangementList(sel);
                        }
                    }

                    RemoveItemFromInventoryList(sel);

                    RedrawInventoryList();
                }
            }
        }

        void RemoveItemFromInventoryList(WorkOrderViewModel dto)
        {
            if(workOrderInventoryList.Where(a => a.InventoryName == dto.InventoryName && a.Size == dto.Size && a.Quantity == dto.Quantity).Any())
            {
                WorkOrderInventoryItemDTO remove = workOrderInventoryList.Where(a => a.InventoryName == dto.InventoryName && a.Size == dto.Size && a.Quantity == dto.Quantity).First();

                workOrderInventoryList.Remove(remove);
            }
        }

        void RemoveItemFromArrangementList(WorkOrderViewModel dto)
        {
            foreach(AddArrangementRequest aaReq in arrangementList)
            {
                if (dto.InventoryId == 0)  //not in inventory
                {
                    if(aaReq.NotInInventory.Where(a => a.NotInInventoryName == dto.InventoryName && a.NotInInventorySize == dto.Size && a.NotInInventoryQuantity == dto.Quantity).Any())
                    {
                        NotInInventoryDTO remove = aaReq.NotInInventory.Where(a => a.NotInInventoryName == dto.InventoryName && a.NotInInventorySize == dto.Size && a.NotInInventoryQuantity == dto.Quantity).First();
                        aaReq.NotInInventory.Remove(remove);
                    }
                }
                else
                {
                    if (aaReq.ArrangementInventory.Where(a => a.InventoryName == dto.InventoryName && a.Size == dto.Size && a.Quantity == dto.Quantity).Any())
                    {
                        ArrangementInventoryItemDTO remove = aaReq.ArrangementInventory.Where(a => a.InventoryName == dto.InventoryName && a.Size == dto.Size && a.Quantity == dto.Quantity).First();
                        aaReq.ArrangementInventory.Remove(remove);
                    }
                }
            }
        }

        public void OnInventorySearchClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(ArrangementFilterPage)))
            {
                Navigation.PushAsync(new ArrangementFilterPage(this));
            }
        }

        public void OnCustomerSearchClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(PersonFilterPage)))
            {
                SearchedForPersonType = 0;
                Navigation.PushAsync(new PersonFilterPage(this));
            }
        }

        private string ValidateSaveWorkOrder()
        {
            string errorMessage = String.Empty;

            if(workOrderInventoryList.Count == 0 && notInInventory.Count == 0 && arrangementList.Count == 0)
            {
                errorMessage += "Please add at least one product. \n";
            }

            //if(Seller.SelectedIndex < 0)
            //{
            //    errorMessage += "Please add the seller's name. \n";
            //}

            //if (String.IsNullOrEmpty(Buyer.Text))
            //{
            //    errorMessage += "Please add the buyer's name. \n";
            //}

            return errorMessage;
        }

        public void OnSaveWorkOrder(object sender, EventArgs e)
        {
            Save.IsEnabled = false;

            string errorMessage = ValidateSaveWorkOrder();

            if(String.IsNullOrEmpty(errorMessage))
            {
                AddWorkOrder();
            }
            else
            {
                DisplayAlert("Error saving work order", errorMessage, "Ok");
            }

            Save.IsEnabled = true;
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

            this.workOrderList.Clear();
            this.arrangementList.Clear();
            this.notInInventory.Clear();
            this.workOrderInventoryList.Clear();
            this.InventoryItemsListView.ItemsSource = workOrderList;
        }

        public bool AddWorkOrder(bool displayAlert = true)
        {
            bool result = false;

            AddWorkOrderRequest addWorkOrderRequest = new AddWorkOrderRequest();
             
            WorkOrderDTO dto = new WorkOrderDTO()
            {
                WorkOrderId = currentWorkOrderId,
                Seller = ((KeyValuePair<long, string>)this.Seller.SelectedItem).Value,
                Buyer = this.Buyer.Text,
                CreateDate = DateTime.Now,
                DeliveryDate = this.DeliveryDate.Date,
                Comments = this.Comments.Text,
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

            addWorkOrderRequest.NotInInventory = notInInventory;

            List<WorkOrderInventoryMapDTO> workOrderInventoryMap = new List<WorkOrderInventoryMapDTO>();


            foreach (WorkOrderInventoryItemDTO woii in workOrderInventoryList)
            {
                WorkOrderViewModel wovm = null; 
                if(((ObservableCollection<WorkOrderViewModel>)InventoryItemsListView.ItemsSource).Where(a => a.InventoryId == woii.InventoryId).Any())
                {
                    wovm = ((ObservableCollection<WorkOrderViewModel>)InventoryItemsListView.ItemsSource).Where(a => a.InventoryId == woii.InventoryId).First();
                }

                workOrderInventoryMap.Add(new WorkOrderInventoryMapDTO()
                {
                    WorkOrderId = currentWorkOrderId,
                    InventoryId = woii.InventoryId,
                    InventoryName = woii.InventoryName,
                    //Quantity = woii.Quantity,
                    Quantity = wovm != null ? wovm.Quantity : 1, 
                    GroupId = woii.GroupId,
                    Size = woii.Size,
                });
            }

            addWorkOrderRequest.WorkOrder = dto;
            addWorkOrderRequest.WorkOrderInventoryMap = workOrderInventoryMap.Where(a => a.GroupId == 0).ToList();

            foreach(NotInInventoryDTO notIn in notInInventory)
            {
                WorkOrderViewModel wovm = null;

                if (((ObservableCollection<WorkOrderViewModel>)InventoryItemsListView.ItemsSource).Where(a => a.NotInInventoryId == notIn.NotInInventoryId).Any())
                {
                    wovm = ((ObservableCollection<WorkOrderViewModel>)InventoryItemsListView.ItemsSource).Where(a => a.NotInInventoryId == notIn.NotInInventoryId).First();
                }

                if(wovm != null)
                {
                    notIn.NotInInventoryQuantity = wovm.Quantity;
                }
            }

            addWorkOrderRequest.NotInInventory = notInInventory;

            addWorkOrderRequest.Arrangements = arrangementList;

            currentWorkOrderId = ((App)App.Current).AddWorkOrder(addWorkOrderRequest);

            if(currentWorkOrderId > 0)
            {
                result = true;

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

                Save.IsEnabled = false;
                Payment.IsEnabled = true;

                if (displayAlert)
                {
                    DisplayAlert("Success", "WorkOrder Saved!", "OK");
                }
            }
            else
            {
                if (displayAlert)
                {
                    DisplayAlert("Error", "There was an error saving this work order.", "Ok");
                }
            }

            return result;
        }

        private void Quantity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //quantity has changed in the ObservableCollection - update the actual backing store - what needs to happen is the consolidation of all the different DTO types
            //at the very least, a common base class of WorkOrderViewModel

            Xamarin.Forms.Entry entry = sender as Xamarin.Forms.Entry;

            if (entry != null)
            {
                string strQty = entry.Text;

                if (!String.IsNullOrEmpty(strQty))
                {
                    int qty = Convert.ToInt32(strQty);
                }

                foreach(WorkOrderInventoryItemDTO dto in workOrderInventoryList)
                {
                    WorkOrderViewModel wovm = null;
                    if (((ObservableCollection<WorkOrderViewModel>)InventoryItemsListView.ItemsSource).Where(a => a.InventoryId == dto.InventoryId).Any())
                    {
                        wovm = ((ObservableCollection<WorkOrderViewModel>)InventoryItemsListView.ItemsSource).Where(a => a.InventoryId == dto.InventoryId).First();
                        dto.Quantity = wovm.Quantity;
                    }
                }

                foreach(NotInInventoryDTO dto in notInInventory)
                {
                    WorkOrderViewModel wovm = null;
                    if (((ObservableCollection<WorkOrderViewModel>)InventoryItemsListView.ItemsSource).Where(a => a.InventoryId == dto.NotInInventoryId).Any())
                    {
                        wovm = ((ObservableCollection<WorkOrderViewModel>)InventoryItemsListView.ItemsSource).Where(a => a.NotInInventoryId == dto.NotInInventoryId).First();
                        dto.NotInInventoryQuantity = wovm.Quantity;
                    }
                }
            }
        }

        private void Payment_Clicked(object sender, EventArgs e)
        {
            if (currentWorkOrderId > 0)
            {
                if (!PageExists(typeof(PaymentPage)))
                {
                    if (AddWorkOrder(false))
                    {
                        Navigation.PushAsync(new PaymentPage(currentWorkOrderId, workOrderInventoryList));
                    }
                    else
                    {
                        DisplayAlert("Error", "There was an error saving the work order", "Ok");
                    }
                }
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
            }
        }

        private void Entry_Focused(object sender, FocusEventArgs e)
        {
            //SearchedForPersonType = 1;
            //Navigation.PushModalAsync(new PersonFilterPage(this));
        }

        private void AddInventory_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(ArrangementFilterPage)))
            {
                Navigation.PushAsync(new ArrangementFilterPage(this));
            }
        }

        private void Buyer_Focused(object sender, FocusEventArgs e)
        {
            if (!PageExists(typeof(PersonFilterPage)))
            {
                SearchedForPersonType = 0;
                Navigation.PushAsync(new PersonFilterPage(this));
            }
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
                    if (!PageExists(typeof(PersonFilterPage)))
                    {
                        Navigation.PushAsync(new PersonFilterPage(this));
                    }
                }
                else if(p.SelectedIndex == 2)
                {
                    if (!PageExists(typeof(CustomerPage)))
                    {
                        Navigation.PushAsync(new CustomerPage(this));
                    }
                }
            }
        }

        private void InventoryItemsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            //if the item selected has GroupId != null - the clicked on item is a member in an arrangement
            //reload the entire arrangement back to the Arrangement page.

            ListView lv = sender as ListView;

            if(lv != null && lv.SelectedItem != null)
            {
                WorkOrderViewModel dto = lv.SelectedItem as WorkOrderViewModel;

                if(dto != null)
                {
                    if(dto.GroupId.HasValue)
                    {
                        if (!PageExists(typeof(TabbedArrangementPage)))
                        {
                            AddArrangementRequest aar = arrangementList.Where(a => a.GroupId == dto.GroupId).FirstOrDefault();

                            //get all members with same group id and load Arrangement page
                            Navigation.PushAsync(new TabbedArrangementPage(aar, searchedForPerson));
                        }
                    }
                }
            }
        }

        private bool NotInInventoryItemIsinList(NotInInventoryDTO dto)
        {
            return notInInventory.Where(a => a.NotInInventoryName == dto.NotInInventoryName &&
                a.NotInInventorySize == dto.NotInInventorySize && a.NotInInventoryPrice == dto.NotInInventoryPrice).Any();
        }

        public bool EnableWorkOrderButtons()
        {
            bool enableButtons = true;

            if(currentWorkOrderId > 0)
            {
                if(currentWorkOrderPaymentId > 0)
                {
                    enableButtons = false;
                }
            }

            return enableButtons;
        }
    }
}