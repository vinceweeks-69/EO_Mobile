using Android.Content;
using EOMobile.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SiteServicePage : EOBasePage
    {
        List<WorkOrderInventoryItemDTO> siteServiceInventoryList = new List<WorkOrderInventoryItemDTO>();
        List<UserDTO> users = new List<UserDTO>();
        List<KeyValuePair<long, string>> employeeDDL = new List<KeyValuePair<long, string>>();
        WorkOrderResponse workOrder = new WorkOrderResponse();
        PersonAndAddressDTO searchedForPerson = null;
        TabbedSiteServicePage TabParent;
        long customerId = 0;
        long currentSiteServiceId = 0;
        int searchedForPersonType = 0;

        List<EOImgData> imageData = new List<EOImgData>();

        public SiteServicePage(TabbedSiteServicePage tabParent)
        {
            TabParent = tabParent;
            InitializeComponent();
            Initialize();

            MessagingCenter.Subscribe<PersonAndAddressDTO>(this, "SearchCustomer", (arg) =>
            {
                searchedForPerson = arg as PersonAndAddressDTO;
            });


            MessagingCenter.Subscribe<WorkOrderResponse>(this, "PaymentSuccess", (arg) =>
            {
                OnClear(null, null);
            });
        }

        public SiteServicePage(TabbedSiteServicePage tabParent, long siteServiceId) : this(tabParent)
        {
            //load data with passed id
            workOrder = ((App)App.Current).GetWorkOrder(siteServiceId);
            currentSiteServiceId = workOrder.WorkOrder.WorkOrderId;

            CreatedBy.SelectedIndex = ((App)App.Current).GetPickerIndex(CreatedBy, workOrder.WorkOrder.SellerId);

            ServicedBy.SelectedIndex = ((App)App.Current).GetPickerIndex(ServicedBy, workOrder.WorkOrder.DeliveryUserId);

            Customer.Text = workOrder.WorkOrder.Buyer;

            customerId = workOrder.WorkOrder.CustomerId;

            DeliveryDate.Date = workOrder.WorkOrder.DeliveryDate;

            foreach (var x in workOrder.WorkOrderList)
            {
                siteServiceInventoryList.Add(new WorkOrderInventoryItemDTO()
                {
                    WorkOrderId = x.WorkOrderId,
                    InventoryId = x.InventoryId,
                    InventoryName = x.InventoryName,
                    Quantity = x.Quantity,
                    Size = x.Size
                });
            }

            ObservableCollection<WorkOrderInventoryItemDTO> list1 = new ObservableCollection<WorkOrderInventoryItemDTO>(siteServiceInventoryList);

            SiteServiceInventoryItemsListView.ItemsSource = list1;

            foreach(WorkOrderImageMapDTO imgData in workOrder.ImageMap)
            {
                imageData.Add(new EOImgData()
                {
                   ImageId = imgData.ImageId,
                   imgData = imgData.ImageData
                });
            }
        }

        public void Initialize()
        {
            users = ((App)App.Current).GetUsers();

            foreach (UserDTO user in users)
            {
                employeeDDL.Add(new KeyValuePair<long, string>(user.UserId, user.UserName));
            }

            CreatedBy.ItemsSource = employeeDDL;

            ServicedBy.ItemsSource = employeeDDL;
                        
            Completed.IsEnabled = false;

            if (workOrder.WorkOrder.WorkOrderId != 0)
            {
                if(workOrder.WorkOrder.Paid)
                {
                    Pay.IsEnabled = false;

                    if(!workOrder.WorkOrder.Delivered)
                    {
                        Completed.IsEnabled = true;
                    }
                }
            }
        }

        public int SearchedForPersonType
        {
            get { return searchedForPersonType; }
            set { searchedForPersonType = value; }
        }

        public void OnClear(object sender, EventArgs e)
        {
            currentSiteServiceId = 0;
            CreatedBy.SelectedIndex = -1; 
            ServicedBy.SelectedIndex = -1;
            Customer.Text = "";
            customerId = 0;
            Comments.Text = "";
            Save.IsEnabled = true;
            Pay.IsEnabled = false;
            Completed.IsEnabled = false;
            siteServiceInventoryList.Clear();
            ((App)App.Current).ClearImageData();
            ((App)App.Current).ClearImageDataList();
        }

        //called by TabbedSiteServicePage to load images to the Image page
        //done once at init ssiteservice page is created first so the parent has to call this
        //as part of initialization
        public List<EOImgData> LoadImageData()
        {
            return imageData;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            //((App)App.Current).ClearImageData();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            WorkOrderInventoryItemDTO searchedForInventory = ((App)App.Current).searchedForInventory;

            if (searchedForInventory != null && searchedForInventory.InventoryId != 0)
            {
                if (!siteServiceInventoryList.Contains(searchedForInventory))
                {
                    searchedForInventory.Quantity = 1;

                    siteServiceInventoryList.Add(searchedForInventory);
                    ObservableCollection<WorkOrderInventoryItemDTO> list1 = new ObservableCollection<WorkOrderInventoryItemDTO>();

                    foreach (WorkOrderInventoryItemDTO wo in siteServiceInventoryList)
                    {
                        list1.Add(wo);
                    }

                    SiteServiceInventoryItemsListView.ItemsSource = list1;

                    //SetWorkOrderSalesData();

                    ((App)App.Current).searchedForInventory = null;
                }
            }

            PersonAndAddressDTO searchedForCustomer = ((App)App.Current).searchedForPerson;

            if (searchedForCustomer != null && searchedForCustomer.Person.person_id != 0)
            {
                customerId = searchedForCustomer.Person.person_id;
                Customer.Text = searchedForCustomer.Person.CustomerName;

                ((App)App.Current).searchedForPerson = null;
            }
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
       
        private void TakePictureClicked(object sender, EventArgs e)
        {
            StartCamera();
        }

        private void Quantity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //SetWorkOrderSalesData();
        }

        private void DeleteButton_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                //remove the selected item from the "master" list
                long itemId = Int64.Parse(button.CommandParameter.ToString());

                WorkOrderInventoryItemDTO sel = siteServiceInventoryList.Where(a => a.InventoryId == itemId).FirstOrDefault();

                if (sel.InventoryId != 0)
                {
                    siteServiceInventoryList.Remove(sel);

                    ObservableCollection<WorkOrderInventoryItemDTO> list1 = new ObservableCollection<WorkOrderInventoryItemDTO>();

                    foreach (WorkOrderInventoryItemDTO wo in siteServiceInventoryList)
                    {
                        list1.Add(wo);
                    }

                    SiteServiceInventoryItemsListView.ItemsSource = list1;

                    //SetWorkOrderSalesData();
                }
            }
        }

        public void AddWorkOrder()
        {
            AddWorkOrderRequest addWorkOrderRequest = new AddWorkOrderRequest();

            WorkOrderDTO dto = new WorkOrderDTO()
            {
                WorkOrderId = currentSiteServiceId,
                SellerId = ((KeyValuePair<long, string>)this.CreatedBy.SelectedItem).Key,
                Seller = ((KeyValuePair<long, string>)this.CreatedBy.SelectedItem).Value,
                Buyer = this.Customer.Text,
                DeliveredBy = ((KeyValuePair<long, string>)ServicedBy.SelectedItem).Value,
                CustomerId = customerId,
                CreateDate = DateTime.Now,
                Comments = this.Comments.Text,
                DeliveryUserId = ((KeyValuePair<long, string>)this.ServicedBy.SelectedItem).Key,
                IsSiteService = true,
                DeliveryDate = DeliveryDate.Date
            };

            List<WorkOrderInventoryMapDTO> workOrderInventoryMap = new List<WorkOrderInventoryMapDTO>();

            foreach (WorkOrderInventoryItemDTO woii in siteServiceInventoryList)
            {
                workOrderInventoryMap.Add(new WorkOrderInventoryMapDTO()
                {
                    WorkOrderId = currentSiteServiceId,
                    InventoryId = woii.InventoryId,
                    InventoryName = woii.InventoryName,
                    Quantity = woii.Quantity
                });
            }

            List<EOImageSource> imgSource = TabParent.GetImages();

            foreach (EOImageSource img in imgSource)
            {
                addWorkOrderRequest.ImageMap.Add(new WorkOrderImageMapDTO()
                {
                    WorkOrderId = currentSiteServiceId,
                    ImageId = img.ImageId,
                    ImageData = img.Image
                });
            }

            addWorkOrderRequest.WorkOrder = dto;
            addWorkOrderRequest.WorkOrderInventoryMap = workOrderInventoryMap;

            long newWorkOrderId = ((App)App.Current).AddWorkOrder(addWorkOrderRequest);

            if (newWorkOrderId > 0)
            {
                //imageData.Clear();
                //((App)App.Current).ClearImageData();
                //this.siteServiceInventoryList.Clear();
                //this.SiteServiceInventoryItemsListView.ItemsSource = null;
                if (!workOrder.WorkOrder.Paid)
                {
                    Pay.IsEnabled = true;
                }
            }
            else
            {
                //DisplayAlert(?)
            }
        }

        private void SaveButton_Clicked(object sender, EventArgs e)
        {
            AddWorkOrder();
        }

        private void AddInventory_Clicked(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new ArrangementFilterPage(this));
        }

        private void Customer_Focused(object sender, FocusEventArgs e)
        {
            SearchedForPersonType = 0;
            Navigation.PushModalAsync(new PersonFilterPage(this));
        }

        private void Completed_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //need this? press "Completed" and then "Save"
        }

        private void Pay_Clicked(object sender, EventArgs e)
        {
            if (currentSiteServiceId > 0)
            {
                Navigation.PushModalAsync(new PaymentPage(currentSiteServiceId, siteServiceInventoryList));
            }
        }
    }
}