using EOMobile.Interfaces;
using EOMobile.ViewModels;
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
	public partial class WorkOrderPage : EOBasePage
	{
        //if a work order contains an arrangement, save that data here
        private List<AddArrangementRequest> arrangementList = new List<AddArrangementRequest>();

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

        public PersonAndAddressDTO Customer
        {
            get { return searchedForPerson; }
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

            ((App)App.Current).GetUsers().ContinueWith(a => LoadUsers(a.Result));

            Save.IsEnabled = true;

            //both buttons are disabled until the work order data is saved
            Payment.IsEnabled = false;
            //PaymentType.IsEnabled = false;
        }

        private void LoadUsers(GetUserResponse userResponse)
        {
            users = userResponse.Users;

            foreach (UserDTO user in users)
            {
                employeeDDL.Add(new KeyValuePair<long, string>(user.UserId, user.UserName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                DeliveryPerson.ItemsSource = employeeDDL;

                DeliveryPersonLabel.Text = "";
                DeliveryPerson.IsVisible = false;

                DeliverToLabel.Text = "";
                DeliverTo.IsVisible = false;

                DeliveryDateLabel.Text = "Pickup Date";
                DeliveryDate.IsVisible = true;

                Seller.ItemsSource = employeeDDL;
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

        private void WorkOrderPaymentLoaded(WorkOrderResponse workOrder, WorkOrderPaymentDTO payment)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
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
                Seller.SelectedIndex = ((App)App.Current).GetPickerIndex(Seller, workOrder.WorkOrder.SellerId);

                DeliveryType.SelectedIndex = workOrder.WorkOrder.DeliveryType;

                deliveryUserId = workOrder.WorkOrder.DeliveryUserId;
                DeliveryPerson.SelectedIndex = ((App)App.Current).GetPickerIndex(DeliveryPerson, workOrder.WorkOrder.DeliveryUserId);

                DeliveryDate.Date = workOrder.WorkOrder.DeliveryDate;

                deliveryRecipientId = workOrder.WorkOrder.DeliveryRecipientId;

                DeliverTo.Text = workOrder.WorkOrder.DeliverTo;

                WorkOrderDate.Date = workOrder.WorkOrder.CreateDate;

                ObservableCollection<WorkOrderViewModel> list1 = new ObservableCollection<WorkOrderViewModel>();

                Dictionary<long, List<WorkOrderInventoryItemDTO>> arrangements = new Dictionary<long, List<WorkOrderInventoryItemDTO>>();

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

                    if (dto.GroupId.HasValue && dto.GroupId != 0)
                    {
                        if (!arrangements.ContainsKey(dto.GroupId.Value))
                        {
                            List<WorkOrderInventoryItemDTO> dtos = new List<WorkOrderInventoryItemDTO>();
                            dtos.Add(dto);
                            arrangements.Add(dto.GroupId.Value, dtos);
                        }
                        else
                        {
                            arrangements[dto.GroupId.Value].Add(dto);
                        }
                    }
                    else
                    {
                        workOrderInventoryList.Add(dto);
                        list1.Add(new WorkOrderViewModel(dto));
                    }
                }

                foreach(KeyValuePair<long,List<WorkOrderInventoryItemDTO>> kvp in arrangements)
                {
                    //add a blank row
                    list1.Add(new WorkOrderViewModel()
                    {
                        WorkOrderId = kvp.Value[0].WorkOrderId,
                        InventoryId = 0,
                        InventoryName = String.Empty,
                        Quantity = 0,
                        Size = String.Empty,
                        GroupId = kvp.Value[0].GroupId
                    });

                    //add a Header row
                    list1.Add(new WorkOrderViewModel()
                    {
                        WorkOrderId = kvp.Value[0].WorkOrderId,
                        InventoryId = 0,
                        InventoryName = "Arrangement",
                        Quantity = 0,
                        Size = String.Empty,
                        GroupId = kvp.Value[0].GroupId
                    });

                    foreach (WorkOrderInventoryItemDTO d in kvp.Value)
                    {
                        list1.Add(new WorkOrderViewModel()
                        {
                            WorkOrderId = d.WorkOrderId,
                            InventoryId = d.InventoryId,
                            InventoryName = d.InventoryName,
                            Quantity = d.Quantity,
                            Size = d.Size,
                            GroupId = d.GroupId
                        });
                    }

                    //add a blank row
                    list1.Add(new WorkOrderViewModel()
                    {
                        WorkOrderId = kvp.Value[0].WorkOrderId,
                        InventoryId = 0,
                        InventoryName = String.Empty,
                        Quantity = 0,
                        Size = String.Empty,
                        GroupId = kvp.Value[0].GroupId
                    });
                }

                InventoryItemsListView.ItemsSource = list1;
            });
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

                        ObservableCollection<WorkOrderViewModel> list1 = new ObservableCollection<WorkOrderViewModel>();

                        foreach (WorkOrderInventoryItemDTO wo in workOrderInventoryList)
                        {
                            list1.Add(new WorkOrderViewModel(wo));
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

            GetSearchedArrangement();
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

        /// <summary>
        /// Called when arrangement data is "added" to a work order - need to use groupId variable in arrangement DTOs if PKs don't yet exist
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

                //if the arrangement data was passed back to the Arrangement page for any reason, clean the local store
                List<WorkOrderInventoryItemDTO> toRemove = workOrderInventoryList.Where(a => a.GroupId == groupId).ToList();
                
                foreach(WorkOrderInventoryItemDTO remove in toRemove)
                {
                    workOrderInventoryList.Remove(remove);
                }

                //add blank row at start
                WorkOrderInventoryItemDTO blankFirst = new WorkOrderInventoryItemDTO();
                blankFirst.GroupId = groupId;
                workOrderInventoryList.Add(blankFirst);

                //add a "Header" row
                WorkOrderInventoryItemDTO header = new WorkOrderInventoryItemDTO();
                header.GroupId = groupId;
                header.InventoryName = "Arrangement";
                workOrderInventoryList.Add(header);

                //translate between the two inventory types
                foreach (ArrangementInventoryDTO dto in aar.ArrangementInventory)
                {
                    WorkOrderInventoryItemDTO wdto = new WorkOrderInventoryItemDTO(dto);
                    wdto.GroupId = groupId;
                    workOrderInventoryList.Add(wdto);
                }

                //add blank row at end
                WorkOrderInventoryItemDTO blankLast = new WorkOrderInventoryItemDTO();
                blankLast.GroupId = groupId;
                workOrderInventoryList.Add(blankLast);

                ObservableCollection<WorkOrderViewModel> list1 = new ObservableCollection<WorkOrderViewModel>();

                foreach (WorkOrderInventoryItemDTO wo in workOrderInventoryList)
                {
                    list1.Add(new WorkOrderViewModel(wo));
                }

                if(!arrangementList.Where(a => a.GroupId == groupId).Any())
                {
                    arrangementList.Add(aar);
                }
                else
                {
                    AddArrangementRequest old = arrangementList.Where(a => a.GroupId == groupId).FirstOrDefault();
                    int index = arrangementList.IndexOf(old);
                    if(index >= 0 && index < arrangementList.Count)
                    {
                        arrangementList[index] = aar;
                    }
                }

                InventoryItemsListView.ItemsSource = list1;

                ((App)App.Current).searchedForArrangement = null;
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

            if(workOrderInventoryList.Count == 0)
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
            string errorMessage = ValidateSaveWorkOrder();

            if(String.IsNullOrEmpty(errorMessage))
            {
                AddWorkOrder();
            }
            else
            {
                DisplayAlert("Error saving work order", errorMessage, "Ok");
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
                    Quantity = woii.Quantity,
                    GroupId = woii.GroupId
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

                DisplayAlert("Success", "WorkOrder Saved!", "OK");

                OnClear(null, new EventArgs());
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
                if (!PageExists(typeof(PaymentPage)))
                {
                    Navigation.PushAsync(new PaymentPage(currentWorkOrderId, workOrderInventoryList));
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
                            Navigation.PushAsync(new TabbedArrangementPage(aar));
                        }
                    }
                }
            }
        }
    }
}