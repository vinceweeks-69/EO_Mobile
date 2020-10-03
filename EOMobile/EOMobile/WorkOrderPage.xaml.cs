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

        public List<GetArrangementResponse> arrangements = new List<GetArrangementResponse>();

        List<NotInInventoryDTO> notInInventory = new List<NotInInventoryDTO>();

        TabbedWorkOrderPage TabParent = null;

        int NotInInventoryTempId = 1;

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

            Save.Clicked += OnSaveWorkOrder;

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

                //Dictionary<long, List<WorkOrderInventoryItemDTO>> arrangements = new Dictionary<long, List<WorkOrderInventoryItemDTO>>();

                notInInventory = workOrder.NotInInventory;

                workOrder.NotInInventory.Where(b => b.ArrangementId == null || b.ArrangementId == 0).ToList().ForEach(x =>
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
                            GroupId = x.ArrangementId
                        };

                    list1.Add(new WorkOrderViewModel(dto));

                });

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
                    list1.Add(new WorkOrderViewModel(dto));
                }

                arrangements = workOrder.Arrangements;

                foreach(GetArrangementResponse ar in workOrder.Arrangements)
                {
                    //add a blank row
                    list1.Add(new WorkOrderViewModel()
                    {
                        WorkOrderId = workOrder.WorkOrder.WorkOrderId,
                        InventoryId = 0,
                        InventoryName = String.Empty,
                        Quantity = 0,
                        Size = String.Empty,
                        GroupId = ar.ArrangementList[0].ArrangementId
                    });

                    //add a Header row
                    list1.Add(new WorkOrderViewModel()
                    {
                        WorkOrderId = workOrder.WorkOrder.WorkOrderId,
                        InventoryId = 0,
                        InventoryName = "Arrangement",
                        Quantity = 0,
                        Size = String.Empty,
                        GroupId = ar.ArrangementList[0].ArrangementId
                    });

                    ar.ArrangementList.Where(a => a.InventoryId != 0).ToList().ForEach(aid =>
                    {
                        list1.Add(new WorkOrderViewModel()
                        {
                            WorkOrderId = workOrder.WorkOrder.WorkOrderId,
                            InventoryId = aid.InventoryId,
                            InventoryName = aid.ArrangementInventoryName,
                            Quantity = aid.Quantity,
                            Size = aid.Size,
                            GroupId = aid.ArrangementId
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
                               GroupId = x.ArrangementId
                           };

                       list1.Add(new WorkOrderViewModel(dto));

                   });

                    //add a blank row
                    list1.Add(new WorkOrderViewModel()
                    {
                        WorkOrderId = workOrder.WorkOrder.WorkOrderId,
                        InventoryId = 0,
                        InventoryName = String.Empty,
                        Quantity = 0,
                        Size = String.Empty,
                        GroupId = ar.ArrangementList[0].ArrangementId
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

                ObservableCollection<WorkOrderViewModel> list1 = new ObservableCollection<WorkOrderViewModel>();

                //if the arrangement data was passed back to the Arrangement page for any reason, clean the local store
                //List<WorkOrderInventoryItemDTO> toRemove = workOrderInventoryList.Where(a => a.GroupId == groupId).ToList();

                //foreach(WorkOrderInventoryItemDTO remove in toRemove)
                //{
                //    workOrderInventoryList.Remove(remove);
                //}

                notInInventory.ToList().ForEach(item =>
                {
                    WorkOrderInventoryItemDTO wovm = new WorkOrderInventoryItemDTO();
                    wovm.GroupId = item.ArrangementId;
                    wovm.InventoryId = 0;
                    wovm.InventoryName = item.NotInInventoryName;
                    wovm.Quantity = item.NotInInventoryQuantity;
                    wovm.Size = item.NotInInventorySize;
                    wovm.WorkOrderId = item.WorkOrderId;

                    workOrderInventoryList.Add(wovm);
                });

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

                    if(dto.InventoryId == 0)  //item not in inventory
                    {
                        wdto.InventoryName = dto.ArrangementInventoryName;
                        wdto.NotInInventoryName = dto.ArrangementInventoryName;
                        wdto.NotInInventorySize = dto.Size;
                        wdto.Quantity = dto.Quantity;
                        //wdto.NotInInventoryPrice = dto.Price;  //add price to ArrangementInventoryItemDTO ?
                        wdto.GroupId = groupId;
                    }

                    workOrderInventoryList.Add(wdto);
                }

                aar.NotInInventory.Where(a => a.ArrangementId != 0).ToList().ForEach(item =>
                {
                    WorkOrderInventoryItemDTO wdto = new WorkOrderInventoryItemDTO();

                    wdto.InventoryName = item.NotInInventoryName;
                    wdto.NotInInventoryName = item.NotInInventoryName;
                    wdto.NotInInventorySize = item.NotInInventorySize;
                    wdto.Quantity = item.NotInInventoryQuantity;
                    //wdto.NotInInventoryPrice = dto.Price;  //add price to ArrangementInventoryItemDTO ?
                    wdto.GroupId = item.ArrangementId;

                    workOrderInventoryList.Add(wdto);
                });

                //add blank row at end
                WorkOrderInventoryItemDTO blankLast = new WorkOrderInventoryItemDTO();
                blankLast.GroupId = groupId;
                workOrderInventoryList.Add(blankLast);

                

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

                AddArrangementRequest temp = arrangementList.Where(a => a.GroupId == aar.GroupId).FirstOrDefault();
                
                if(temp != null)
                {
                    arrangementList.Remove(temp);
                }
                
                arrangementList.Add(aar);

                //no duplicates, please 
                if(arrangements.Where(a => a.Arrangement.ArrangementId == aar.Arrangement.ArrangementId).Any())
                {
                    GetArrangementResponse remove = arrangements.Where(a => a.Arrangement.ArrangementId == aar.Arrangement.ArrangementId).First();

                    arrangements.Remove(remove);
                }

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

            List<NotInInventoryDTO> notInInventory = new List<NotInInventoryDTO>();

            foreach (WorkOrderInventoryItemDTO woii in workOrderInventoryList)
            {
                if (!String.IsNullOrEmpty(woii.NotInInventoryName))
                {
                    //don't add the items that are members of an arrangement
                    if (!woii.GroupId.HasValue || woii.GroupId == 0)
                    {
                        //notInInventory.Add(new NotInInventoryDTO()
                        //{
                        //    WorkOrderId = currentWorkOrderId,
                        //    ArrangementId = woii.GroupId,
                        //    NotInInventoryName = woii.NotInInventoryName,
                        //    NotInInventorySize = woii.NotInInventorySize,
                        //    NotInInventoryQuantity = woii.Quantity,
                        //    NotInInventoryPrice = woii.NotInInventoryPrice
                        //});
                    }
                }
                else
                {
                    //don't add the items that are members of an arrangement
                    if (!woii.GroupId.HasValue || woii.GroupId == 0)
                    {
                        workOrderInventoryMap.Add(new WorkOrderInventoryMapDTO()
                        {
                            WorkOrderId = currentWorkOrderId,
                            InventoryId = woii.InventoryId,
                            InventoryName = woii.InventoryName,
                            Quantity = woii.Quantity,
                            GroupId = woii.GroupId,
                            Size = woii.Size,
                        });
                    }
                }
            }

            addWorkOrderRequest.WorkOrder = dto;
            addWorkOrderRequest.WorkOrderInventoryMap = workOrderInventoryMap.Where(a => a.GroupId == 0).ToList();
            addWorkOrderRequest.NotInInventory = notInInventory;
            addWorkOrderRequest.Arrangements = arrangementList;

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

                            if(aar is null && arrangements.Where(a => a.Arrangement.ArrangementId == dto.GroupId).Any())
                            {
                                GetArrangementResponse ar = arrangements.Where(a => a.Arrangement.ArrangementId == dto.GroupId).First();
                                aar = new AddArrangementRequest();
                                aar.Arrangement = ar.Arrangement;
                                aar.ArrangementInventory = ar.ArrangementList.Where(a => a.InventoryId != 0).ToList();
                                aar.GroupId = ar.Arrangement.ArrangementId;
                                aar.NotInInventory = ar.NotInInventory;
                            }

                            //get all members with same group id and load Arrangement page
                            Navigation.PushAsync(new TabbedArrangementPage(aar));
                        }
                    }
                }
            }
        }

        private void AddItemNotInInventory_Clicked(object sender, EventArgs e)
        {
            String msg = String.Empty;
            if (NotInInventoryName.Text == String.Empty)
            {
                msg += "Please add a name for the item not in inventory. \n";
            }

            if(NotInInventorySize.Text == String.Empty)
            {
                msg += "Please add a size for the item not in inventory. \n";
            }

            if (NotInInventoryQuantity.Text == String.Empty)
            {
                msg += "Please add a quantity for the item not in inventory. \n";
            }

            if (NotInInventoryPrice.Text == String.Empty)
            {
                msg += "Please add a price for the item not in inventory. \n";
            }

            if(msg!= String.Empty)
            {
                DisplayAlert("Error", msg, "Ok");
            }
            else
            {
                NotInInventoryDTO dto = new NotInInventoryDTO();
                dto.ArrangementId = 0;
                dto.NotInInventoryId = 0;
                dto.NotInInventoryName = NotInInventoryName.Text;
                dto.NotInInventorySize = NotInInventorySize.Text;
                dto.NotInInventoryPrice = Convert.ToDecimal(NotInInventoryPrice.Text);

                if(!NotInInventoryItemIsinList(dto))
                {
                    notInInventory.Add(dto);

                    ObservableCollection<WorkOrderInventoryItemDTO> list1 = new ObservableCollection<WorkOrderInventoryItemDTO>();

                     foreach (WorkOrderInventoryItemDTO wo in workOrderInventoryList)
                     {
                          list1.Add(wo);
                     }

                    foreach(NotInInventoryDTO nid in notInInventory)
                    {
                        list1.Add(new WorkOrderInventoryItemDTO()
                        {
                           GroupId = 0,
                           InventoryId = 0,
                           InventoryName = nid.NotInInventoryName,
                           NotInInventoryName = nid.NotInInventoryName,
                           NotInInventoryPrice = nid.NotInInventoryPrice,
                           Quantity = nid.NotInInventoryQuantity,
                           NotInInventorySize = nid.NotInInventorySize,
                           Size = nid.NotInInventorySize,
                           WorkOrderId = currentWorkOrderId
                        });
                    }

                    InventoryItemsListView.ItemsSource = list1;

                    NotInInventoryName.Text = String.Empty;
                    NotInInventorySize.Text = String.Empty;
                    NotInInventoryPrice.Text = String.Empty;
                    NotInInventoryQuantity.Text = String.Empty;
                }
                else
                {
                    DisplayAlert("Error", "Duplicate item", "Ok");
                }
            }
        }

        private bool NotInInventoryItemIsinList(NotInInventoryDTO dto)
        {
            return notInInventory.Where(a => a.NotInInventoryName == dto.NotInInventoryName &&
                a.NotInInventorySize == dto.NotInInventorySize && a.NotInInventoryPrice == dto.NotInInventoryPrice).Any();
        }

        //private bool NotInInventoryItemIsinList(WorkOrderInventoryItemDTO dto)
        //{
        //    return workOrderInventoryList.Where(a => a.NotInInventoryName == dto.NotInInventoryName &&
        //        a.NotInInventorySize == dto.NotInInventorySize && a.NotInInventoryPrice == dto.NotInInventoryPrice).Any();
        //}
    }
}