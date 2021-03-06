﻿using EOMobile.Interfaces;
using EOMobile.ViewModels;
using SharedData;
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
    //This page is used in multiple modes - Arrangements can be accessed from the Home page - this is used to create / modify arrangements
    //in this mode, an arrangement MUST be named  and when the "Save" button is pressed, the arrangement data is saved to the db.
    //This page can be accessed from the Work Orders page as well. In this mode, an arrangement can be "constructed" and NOT named 
    //(when the "Save" button is pressed, program flow goes back to the Work Order Page and the current arrangement is 
    //passed as part of the change in page navigation) - the constituent parts of the arrangement to be added to the work order are passed back 
    //to the Work Order page and kept in memory. A container value MUST be set. For arrangement creation, the type MUST be new container. 
    //For work order mode, Container can be any of the 3 possible values. The "save " button will not work unless these conditions are met.

	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ArrangementPage : EOBasePage
	{
        List<ArrangementInventoryItemDTO> arrangementInventoryList = new List<ArrangementInventoryItemDTO>();
        List<GetSimpleArrangementResponse> arrangementList = new List<GetSimpleArrangementResponse>();

        //ObservableCollection<ArrangementInventoryItemDTO> arrangementInventoryListOC = new ObservableCollection<ArrangementInventoryItemDTO>();
        ObservableCollection<GetSimpleArrangementResponse> arrangementListOC = new ObservableCollection<GetSimpleArrangementResponse>();

        TabbedArrangementPage TabParent = null;

        List<long> inventoryImageIdsLoaded = new List<long>();

        long? customerContainerId = null;

        long NotInInventoryTempId = 0;

        List<NotInInventoryDTO> notInInventoryList = new List<NotInInventoryDTO>();

        List<CustomerContainerDTO> customerContainers = new List<CustomerContainerDTO>();

        ObservableCollection<KeyValuePair<long, string>> containers = new ObservableCollection<KeyValuePair<long, string>>();

        /// <summary>
        /// If the TabbedParent has a CurrentArrangement value, load the form with these values
        /// </summary>
        /// <param name="tabParent"></param>
        public ArrangementPage (TabbedArrangementPage tabParent)
		{
			InitializeComponent ();

            arrangementList = new List<GetSimpleArrangementResponse>(); 

            foreach(GetSimpleArrangementResponse ar in arrangementList)
            {
                arrangementListOC.Add(ar);
            }

            ArrangementListView.ItemsSource = arrangementListOC;

            GetUsers();

            ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();
            list2.Add(new KeyValuePair<long, string>(1, "180"));
            list2.Add(new KeyValuePair<long, string>(2, "360"));

            Style.ItemsSource = list2;
            
            containers.Add(new KeyValuePair<long, string>(1, "New container"));
            
            Container.ItemsSource = containers;

            EnableCustomerContainerSecondaryControls(false);

            TabParent = tabParent;

            GiftCheckBox.IsChecked = false;

            ArrangementListView.ItemsSource = new ObservableCollection<WorkOrderViewModel>();

            ArrangementListView.ItemSelected += ArrangementListView_ItemSelected;

            TaskAwaiter t = ((App)App.Current).GetCustomerContainers(TabParent.Customer.Person.person_id).ContinueWith(a => CustomerContainersLoaded(a.Result)).GetAwaiter();

            t.OnCompleted(() =>
            {
                CompleteInitialization();
            });
        }

        private void CompleteInitialization()
        {
            //just because there is a Current Arrangement, DOES NOT mean that there is a customer
            //with or without a CustomerContainer - the loading of customer containers must precede this 
            //and must be waited for since it is async
            if (TabParent.CurrentArrangement != null)
            {
                LoadWorkOrderArrangement();
            }

            Container.SelectedIndexChanged += Container_SelectedIndexChanged;
        }

        private async void GetUsers()
        {
            GenericGetRequest request = new GenericGetRequest("GetUsers", String.Empty, 0);
            ((App)App.Current).GetRequest<GetUserResponse>(request).ContinueWith(a => UsersLoaded(a.Result));
        }

        private void UsersLoaded(GetUserResponse response)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();
                foreach (UserDTO u in response.Users.Where(a => a.RoleId == 2).ToList())
                {
                    list1.Add(new KeyValuePair<long, string>(u.UserId, u.UserName));
                }

                Designer.ItemsSource = list1;
            });
        }

        private void CustomerContainersLoaded(CustomerContainerResponse result)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                customerContainers = result.CustomerContainers;

                if (customerContainers.Count > 0)
                {
                    EnableCustomerContainerSecondaryControls(true);

                    containers.Add(new KeyValuePair<long, string>(2, "Customer container at EO"));
                    containers.Add(new KeyValuePair<long, string>(3, "Customer container at customer site")); //means use liner
                }
            });
        }

        private void LoadWorkOrderArrangement()
        {
            //load data passed from parent
            Name.Text = TabParent.CurrentArrangement.Arrangement.ArrangementName;

            Location.Text = TabParent.CurrentArrangement.Arrangement.LocationName;

            GiftCheckBox.IsChecked = TabParent.CurrentArrangement.Arrangement.IsGift == 1 ? true : false;

            GiftMessage.Text = TabParent.CurrentArrangement.Arrangement.GiftMessage;

            int index = 0;
            foreach(KeyValuePair<long,string> kvp in Designer.ItemsSource)
            {
                if(kvp.Value == TabParent.CurrentArrangement.Arrangement.DesignerName)
                {
                    Designer.SelectedIndex = index;
                    break;
                }
                index++;
            }

            index = 0;
            foreach (KeyValuePair<long, string> kvp in Style.ItemsSource)
            {
                if (kvp.Key == TabParent.CurrentArrangement.Arrangement._180or360)
                {
                    Style.SelectedIndex = index;
                    break;
                }
                index++;
            }

            index = 0;
            foreach (KeyValuePair<long, string> kvp in Container.ItemsSource)
            {
                if (kvp.Key == TabParent.CurrentArrangement.Arrangement.Container + 1)  //match up - list index is 0 based - container values are 1 based 
                {
                    Container.SelectedIndex = index;

                    if(index > 0)
                    {
                        if(customerContainers.Count > 0)
                        {
                            CustomerContainerDTO customerContainerDTO = customerContainers.Where(a => a.CustomerContainerId == TabParent.CurrentArrangement.Arrangement.CustomerContainerId).FirstOrDefault();

                            if(customerContainerDTO != null)
                            {
                                CustomerContainerLabelEntry.Text = customerContainerDTO.Label;
                            }
                        }
                    }
                    break;
                }
                index++;
            }

            arrangementInventoryList = TabParent.CurrentArrangement.ArrangementInventory;

            foreach(NotInInventoryDTO nii in TabParent.CurrentArrangement.NotInInventory)
            {
                notInInventoryList.Add(nii);

                if (arrangementInventoryList.Where(a => a.ArrangementId == nii.ArrangementId && a.InventoryName == nii.NotInInventoryName &&
                    a.Quantity == nii.NotInInventoryQuantity && a.Size == nii.NotInInventorySize).Any())
                {
                    continue;
                }

                arrangementInventoryList.Add(new ArrangementInventoryItemDTO
                {
                    ArrangementId = TabParent.CurrentArrangement.Arrangement.ArrangementId,
                    InventoryName = nii.NotInInventoryName,
                    Quantity = nii.NotInInventoryQuantity,
                    Size = nii.NotInInventorySize,
                });
            }

            if(TabParent.CurrentArrangement.Arrangement.CustomerContainerId.HasValue)
            {
                customerContainerId = TabParent.CurrentArrangement.Arrangement.CustomerContainerId;
                Container.SelectedIndex = TabParent.CurrentArrangement.Arrangement.Container;
            }

            ReloadListData();
        }

        private void EnableCustomerContainerSecondaryControls(bool shouldShow)
        {
            CustomerContainerLabel.IsVisible = shouldShow;
            CustomerContainerLabelEntry.IsVisible = shouldShow;
        }

        private void Container_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if container type is "Customer container at EO" pick from  customer containers on site

            //if container type is "Customer container at customer site" means use liner

            //if container type is "New container" pick container from inventory

            Picker p = sender as Picker;

            if(p != null)
            {
                if(p.SelectedIndex == 0)
                {
                    EnableCustomerContainerSecondaryControls(false);
                }
                else
                {
                    EnableCustomerContainerSecondaryControls(true);

                    if (!PageExists(typeof(CustomerContainerPage)))
                    {
                        Navigation.PushAsync(new CustomerContainerPage(TabParent.Customer, TabParent.ForWorkOrder));
                    }
                }
            }
        }

        private void PickCustomerContainer()
        {
            //if this is WorkOrder mode AND a customer ID is present, show CustomerContainerPage
            //If this is WorkOrder mode AND no customer ID, show Customer Page
        }
               
        protected override void OnAppearing()
        {
            base.OnAppearing();

            Products.IsEnabled = true;

            //called when we return from an "Inventory Search"
            ArrangementInventoryItemDTO searchedForInventory = ((App)App.Current).searchedForArrangementInventory;

            if (searchedForInventory != null && searchedForInventory.InventoryId != 0)
            {
                if (!arrangementInventoryList.Contains(searchedForInventory))
                {
                    //arrangementInventoryListOC.Clear();

                    searchedForInventory.Quantity = 1;

                    arrangementInventoryList.Add(searchedForInventory);

                    //foreach (ArrangementInventoryItemDTO a in arrangementInventoryList)
                    //{
                    //    arrangementInventoryListOC.Add(a);
                    //}

                    //ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;

                    SetWorkOrderSalesData();

                    ((App)App.Current).searchedForArrangementInventory = null;
                }
            }

            //called when we return from a "Customer Search"
            CustomerContainerDTO searchedForCustomerContainer = ((App)App.Current).searchedForCustomerContainer;

            if(searchedForCustomerContainer != null && searchedForCustomerContainer.CustomerContainerId != 0)
            {
                customerContainerId = searchedForCustomerContainer.CustomerContainerId;

                CustomerContainerLabelEntry.Text = searchedForCustomerContainer.Label;

                ((App)App.Current).searchedForCustomerContainer = null;
            }

            //called when we return from a  Work Order page click on a previously created arrangement item 

            //called when we return from an "Inventory Search" AND a a not in inventory item wants to be added
            NotInInventoryDTO notInInventory = ((App)App.Current).notInInventory_toAdd;

            if(notInInventory != null && !NotInInventoryItemIsinList(notInInventory))
            {
                notInInventoryList.Add(notInInventory);
                ((App)App.Current).notInInventory_toAdd = null;
            }

            ReloadListData();
        }

        private void ReloadListData()
        {
            ObservableCollection<WorkOrderViewModel> list1 = new ObservableCollection<WorkOrderViewModel>();

            foreach(ArrangementInventoryItemDTO a in arrangementInventoryList)
            {
                list1.Add(new WorkOrderViewModel(a,0));
            }

            foreach(NotInInventoryDTO a in notInInventoryList)
            {
                list1.Add(new WorkOrderViewModel(a));
            }

            ArrangementItemsListView.ItemsSource = list1;
        }

        private void SetWorkOrderSalesData()
        {
            //GetWorkOrderSalesDetailResponse response = GetWorkOrderDetail();

            //SubTotal.Text = response.SubTotal.ToString("C", CultureInfo.CurrentCulture);
            //Tax.Text = response.Tax.ToString("C", CultureInfo.CurrentCulture);
            //Total.Text = response.Total.ToString("C", CultureInfo.CurrentCulture);
        }

        public void OnSearchArrangementsClicked(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(Name.Text))
            {
                if (arrangementList != null)
                {
                    arrangementList.Clear();
                }

                if (arrangementListOC != null)
                {
                    arrangementListOC.Clear();
                }

                ArrangementSearch.IsEnabled = false;

                ((App)App.Current).GetArrangements(Name.Text).ContinueWith(a => ArrangementSearchComplete(a.Result));
            }
            else
            {
                DisplayAlert("Error", "To search arrangements, enter an arrangement name", "Ok");
            }
        }

        private void ArrangementSearchComplete(List<GetSimpleArrangementResponse> getResult)
        {
            arrangementList = getResult;

            foreach (GetSimpleArrangementResponse ar in arrangementList)
            {
                arrangementListOC.Add(ar);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                ArrangementListView.ItemsSource = arrangementListOC;

                ArrangementSearch.IsEnabled = true;
            });
        }

        public void OnClearArrangementsClicked(object sender, EventArgs e)
        {
            ClearArrangements();
        }

        public void ClearArrangements()
        {
            Name.Text = String.Empty;
            Designer.SelectedIndex = -1;
            Style.SelectedIndex = -1;
            Location.Text = String.Empty;
            Container.SelectedIndex = -1;
            CustomerContainerLabelEntry.Text = String.Empty;

            GiftCheckBox.IsChecked = false;

            arrangementList.Clear();
            arrangementListOC.Clear();
            arrangementInventoryList.Clear();
            //arrangementInventoryListOC.Clear();
            //ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;
            inventoryImageIdsLoaded.Clear();
            TabParent.ClearArrangementImages();
            customerContainerId = null;

            EnableCustomerContainerSecondaryControls(false);
        }

        public bool ValidateArrangement(ref string validationMessage)
        {
            if (!TabParent.ForWorkOrder)
            {
                if(String.IsNullOrEmpty(Name.Text))
                {
                    validationMessage += "Please pick a name for this arrangement. \n";
                }
                else
                {
                    ArrangementDTO arrangement = new ArrangementDTO();
                    arrangement.ArrangementId = arrangementInventoryList[0].ArrangementId;
                    arrangement.ArrangementName = Name.Text;
                    //check for duplication
                    if (!((App)App.Current).ArrangementNameIsNotUnique(arrangement))
                    {
                        validationMessage += "This arrangement name is being used. Please choose another. \n";
                    }
                }
            }

            if (Container.SelectedItem == null)
            {
                validationMessage += "Please pick a Container value. \n";
            }
            else
            {
                int containerVal = (int)((KeyValuePair<long, string>)Container.SelectedItem).Key;

                if (containerVal == 1) // "new container"
                {
                    //if the container chosen is not in inventory, this validation cannot be valid
                    if (notInInventoryList.Count == 0)
                    {
                        //check inventory for an inventory item of type container
                        if (!arrangementInventoryList.Where(a => a.InventoryTypeId == 2).Any())
                        {
                            validationMessage += "Please pick a Container. \n";
                        }
                        else
                        {
                            if (arrangementInventoryList.Where(a => a.InventoryTypeId == 2).Count() > 1)
                            {
                                validationMessage += "An arrangement can have only one container. \n";
                            }
                        }
                    }
                }
                else
                {
                    //make sure CustomerContainerId is not null
                    if (!customerContainerId.HasValue || customerContainerId.Value == 0)
                    {
                        validationMessage += "Please choose the customer container to be used \n";
                    }
                }
            }

            return String.IsNullOrEmpty(validationMessage) ? true : false;
        }

        public void OnSaveArrangementsClicked(object sender, EventArgs e)
        {
            try
            {
                string validationMessage = String.Empty;

                long arrangementId = 0;  //(long)((Button)sender).CommandParameter;
               
                if(!ValidateArrangement(ref validationMessage))
                {
                    DisplayAlert("Error",validationMessage,"Cancel");
                    return;
                }

                if (TabParent.ForWorkOrder)
                {
                    //get quantities from ArrangementItemsListView ItemsSource

                    AddArrangementRequest request = new AddArrangementRequest();
                    request.Arrangement = new ArrangementDTO();

                    request.Arrangement.ArrangementId = TabParent.CurrentArrangement != null ? TabParent.CurrentArrangement.Arrangement.ArrangementId : 0;

                    request.Arrangement.ArrangementName = Name.Text;
                    request.Arrangement.DesignerName = Designer.SelectedItem != null ? ((KeyValuePair<long, string>)Designer.SelectedItem).Value : String.Empty;
                    request.Arrangement._180or360 = Style.SelectedItem != null ? (int)((KeyValuePair<long, string>)Style.SelectedItem).Key : 1;
                    request.Arrangement.Container = Container.SelectedItem != null ? (int)((KeyValuePair<long, string>)Style.SelectedItem).Key : 1;   //1 = new container (db default)
                    request.Arrangement.CustomerContainerId = customerContainerId;
                    request.Arrangement.LocationName = Location.Text;
                    request.Arrangement.UpdateDate = DateTime.Now;
                    request.Arrangement.IsGift = GiftCheckBox.IsChecked ? 1 : 0;
                    request.Arrangement.GiftMessage = GiftMessage.Text;

                    Random r = new Random();
                    long tempArrangementId = r.Next(1, 100);

                    foreach (ArrangementInventoryItemDTO dto in arrangementInventoryList)
                    {
                        if (dto.ArrangementId != 0)
                        {
                            arrangementId = dto.ArrangementId;
                            break;
                        }

                        WorkOrderViewModel wovm = null;

                        if (((ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource).Where(a => a.InventoryId == dto.InventoryId).Any())
                        {
                            wovm = ((ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource).Where(a => a.InventoryId == dto.InventoryId).First();
                            dto.Quantity = wovm.Quantity;
                        }
                    }

                    request.ArrangementInventory = arrangementInventoryList;

                    foreach (NotInInventoryDTO dto in notInInventoryList)
                    {
                        //group them for the work order
                        if(!dto.ArrangementId.HasValue || dto.ArrangementId == 0)
                        {
                            dto.ArrangementId = tempArrangementId;
                        }

                        WorkOrderViewModel wovm = null;

                        if(((ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource).Where(a => a.NotInInventoryId == dto.NotInInventoryId).Any())
                        {
                            wovm = ((ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource).Where(a => a.NotInInventoryId == dto.NotInInventoryId).First();
                            dto.NotInInventoryQuantity = wovm.Quantity;
                        }
                    }

                    request.NotInInventory = notInInventoryList;

                    request.Inventory = new InventoryDTO()
                    {
                        InventoryName = String.IsNullOrEmpty(Name.Text) ? "Arrangement_" + Convert.ToString(tempArrangementId) : Name.Text,
                        InventoryTypeId = 5,
                        ServiceCodeId = 365,
                        NotifyWhenLowAmount = 0,
                        Quantity = 1
                    };

                    MessagingCenter.Send<AddArrangementRequest>(request, "AddArrangementToWorkOrder");

                    if(!PopToPage("TabbedWorkOrderPage"))
                    {
                        Navigation.PopAsync();
                    }

                    return;
                }

                if (arrangementInventoryList.Count > 0)
                {
                    foreach (ArrangementInventoryItemDTO dto in arrangementInventoryList)
                    {
                        if (dto.ArrangementId != 0)
                        {
                            arrangementId = dto.ArrangementId;
                            break;
                        }

                        WorkOrderViewModel wovm = null;

                        if (((ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource).Where(a => a.InventoryId == dto.InventoryId).Any())
                        {
                            dto.Quantity = wovm.Quantity;
                        }
                    }

                    if (arrangementId == 0)
                    {
                        AddArrangementRequest request = new AddArrangementRequest();
                        request.Arrangement = new ArrangementDTO();
                        request.Arrangement.ArrangementName = Name.Text;
                        request.Arrangement._180or360 = (int)((KeyValuePair<long,string>)Style.SelectedItem).Key;
                        request.Arrangement.Container = (int)((KeyValuePair<long, string>)Container.SelectedItem).Key;
                        request.Arrangement.CustomerContainerId = null;
                        request.Arrangement.DesignerName = ((KeyValuePair<long, string>)Designer.SelectedItem).Value;
                        request.Arrangement.LocationName = Location.Text;
                        request.Arrangement.IsGift = GiftCheckBox.IsChecked ? 1 : 0;
                        request.Arrangement.GiftMessage = GiftMessage.Text;

                        request.Arrangement.UpdateDate = DateTime.Now;
                        request.ArrangementInventory = arrangementInventoryList.Where(a => a.InventoryId != 0).ToList();  // "Not In Inventory" items may have been added to the display list;

                        request.Inventory = new InventoryDTO()
                        {
                            InventoryName = Name.Text,
                            InventoryTypeId = 5,
                        };

                        request.NotInInventory = notInInventoryList;
                        request.ArrangementInventory = arrangementInventoryList.Where(a => a.InventoryId != 0).ToList();

                        arrangementId = ((App)App.Current).AddArrangement(request);

                        if (arrangementId > 0)
                        {
                            List<EOImgData> imageData = ((App)App.Current).GetImageData();

                            if (imageData.Count > 0)
                            {
                                foreach (EOImgData img in imageData)
                                {
                                    AddArrangementImageRequest imgRequest = new AddArrangementImageRequest()
                                    {
                                        ArrangementId = arrangementId,
                                        Image = img.imgData
                                    };

                                    ((App)App.Current).AddArrangementImage(imgRequest);
                                }
                            }

                            DisplayAlert("Success", "New arrangement saved!", "OK");
                            ClearArrangements();
                        }
                        else
                        {
                            DisplayAlert("Error", "Problem saving arrangement", "OK");
                        }
                    }
                    else
                    {
                        GetSimpleArrangementResponse simpleArrangement = arrangementList.Where(a => a.Arrangement.ArrangementId == arrangementId).FirstOrDefault();

                        AddArrangementRequest request = new AddArrangementRequest();
                        request.Arrangement = new ArrangementDTO();
                        request.Arrangement.ArrangementId = arrangementId;
                        request.Arrangement.ArrangementName = Name.Text;
                        request.Arrangement._180or360 = (int)((KeyValuePair<long, string>)Style.SelectedItem).Key;
                        request.Arrangement.Container = (int)((KeyValuePair<long, string>)Container.SelectedItem).Key;
                        request.Arrangement.CustomerContainerId = null;
                        request.Arrangement.DesignerName = ((KeyValuePair<long, string>)Designer.SelectedItem).Value;
                        request.Arrangement.LocationName = Location.Text;
                        request.Arrangement.UpdateDate = DateTime.Now;
                        request.Arrangement.IsGift = GiftCheckBox.IsChecked ? 1 : 0;
                        request.Arrangement.GiftMessage = GiftMessage.Text;

                        request.Inventory = simpleArrangement.Inventory;

                        request.ArrangementInventory = arrangementInventoryList;

                        request.NotInInventory = notInInventoryList;

                        arrangementId = ((App)App.Current).UpdateArrangement(request);

                        if (arrangementId == request.Arrangement.ArrangementId)
                        {
                            //only saves new images
                            List<EOImgData> imageData = ((App)App.Current).GetImageData();

                            if (imageData.Count > 0)
                            {
                                foreach (EOImgData img in imageData)
                                {
                                    AddArrangementImageRequest imgRequest = new AddArrangementImageRequest()
                                    {
                                        ArrangementId = arrangementId,
                                        Image = img.imgData
                                    };

                                    ((App)App.Current).AddArrangementImage(imgRequest);
                                }
                            }

                            DisplayAlert("Success", "Arrangement updated!", "OK");
                            ClearArrangements();
                        }
                        else
                        {
                            DisplayAlert("Error", "Problem updating arrangement", "OK");
                        }
                    }
                }
            }
            catch(Exception ex)
            {

            }
        }

        public void OnAddImageClicked(object sender, EventArgs e)
        {
            StartCamera();
        }

        public void OnDeleteArrangementItem(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                if (button.CommandParameter != null)
                {
                    WorkOrderViewModel dto = button.CommandParameter as WorkOrderViewModel;

                    if (dto != null)
                    {
                        if (dto.InventoryId == 0)
                        {
                            if (notInInventoryList.Where(a => a.NotInInventoryName == dto.InventoryName && a.NotInInventorySize == dto.Size && a.NotInInventoryQuantity == dto.Quantity).Any())
                            {
                                NotInInventoryDTO nii = notInInventoryList.Where(a => a.NotInInventoryName == dto.InventoryName && a.NotInInventorySize == dto.Size && a.NotInInventoryQuantity == dto.Quantity).First();
                                notInInventoryList.Remove(nii);
                            }
                        }
                        else
                        {
                            if (arrangementInventoryList.Where(a => a.InventoryId == dto.InventoryId).Any())
                            {
                                ArrangementInventoryItemDTO aiid = arrangementInventoryList.Where(a => a.InventoryId == dto.InventoryId).First();
                                arrangementInventoryList.Remove(aiid);
                            }
                        }
                    }

                    ReloadListData();
                }
            }
        }

        private void Quantity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //quantity has changed in the ObservableCollection - update the actual backing store
            //Consolidate the DTOs 

            try
            {
                if (ArrangementItemsListView.ItemsSource == null)
                    return;

                var wtf = (ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource;

                if(wtf != null)
                {
                    foreach (ArrangementInventoryItemDTO dto in arrangementInventoryList)
                    {
                        WorkOrderViewModel wovm = null;
                        if (((ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource).Where(a => a.InventoryId == dto.InventoryId).Any())
                        {
                            wovm = ((ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource).Where(a => a.InventoryId == dto.InventoryId).First();
                            dto.Quantity = wovm.Quantity;
                        }
                    }

                    foreach (NotInInventoryDTO dto in notInInventoryList)
                    {
                        WorkOrderViewModel wovm = null;
                        if (((ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource).Where(a => a.InventoryId == dto.NotInInventoryId).Any())
                        {
                            wovm = ((ObservableCollection<WorkOrderViewModel>)ArrangementItemsListView.ItemsSource).Where(a => a.NotInInventoryId == dto.NotInInventoryId).First();
                            dto.NotInInventoryQuantity = wovm.Quantity;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                int debug = 1;
            }
        }

        public void OnDeleteArrangement(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                if (button.CommandParameter != null)
                {
                    long deleteArrangementId = (long)(button).CommandParameter;
                    ((App)App.Current).DeleteArrangement(deleteArrangementId);

                    //clear image tab sourceList
                    TabParent.ClearArrangementImages();
                }
            }
        }

        private void ArrangementListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ListView lv = sender as ListView;

            if (lv == null|| lv.SelectedItem == null)
                return;

            GetSimpleArrangementResponse item = lv.SelectedItem as GetSimpleArrangementResponse;

            //call GetArrangementsById() and populate form
            ((App)App.Current).GetArrangement(item.Arrangement.ArrangementId).ContinueWith(a => SelectedArrangementLoaded(lv, item, a.Result));

        }

        private void SetPickerSelection(Picker p, string value)
        {

            foreach(KeyValuePair<long,string> kvp in p.ItemsSource as ObservableCollection<KeyValuePair<long,string>>)
            {
                if(kvp.Value == value)
                {
                    p.SelectedItem = kvp;
                }
            }
        }

        private void SelectedArrangementLoaded(ListView lv,GetSimpleArrangementResponse item, GetArrangementResponse response)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Name.Text = response.Arrangement.ArrangementName;

                Location.Text = response.Arrangement.LocationName;

                SetPickerSelection(Designer, item.Arrangement.DesignerName);

                //fix these next 2
                Style.SelectedIndex = item.Arrangement._180or360 - 1;

                Container.SelectedIndex = item.Arrangement.Container - 1;

                //if this is a customer container, there may be a string "label" value 
                //this won't be used in a "save arrangement" scenario, but would be a factor possibly
                //when this page is used to create an arrangement "on the fly" for a work order
                //where the arrangement in question uses a customer's container AND
                //that container has a "label" value.

                //CustomerContainerLabelEntry.Text = item.Arrangement.CustomerContainerLabel;

                arrangementList.Add(item);

                arrangementInventoryList = response.ArrangementList;

                //arrangementInventoryListOC.Clear();

                foreach (ArrangementInventoryItemDTO a in arrangementInventoryList)
                {
                    //arrangementInventoryListOC.Add(a);
                }

                //ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;
                lv.SelectedItem = null;

                TabParent.LoadArrangmentImages(response.Images);
            });
        }

        private void ShowImage_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                if (button.CommandParameter != null)
                {
                    long inventoryId = (long)(button).CommandParameter;
                    ArrangementInventoryItemDTO inventory = arrangementInventoryList.Where(a => a.InventoryId == inventoryId).FirstOrDefault();

                    if (inventory != null && inventory.ImageId != 0)
                    {
                        if (!inventoryImageIdsLoaded.Contains(inventory.ImageId))
                        {
                            EOImgData imageData = ((App)App.Current).GetImage(inventory.ImageId);

                            if (imageData.imgData != null && imageData.imgData.Length > 0)
                            {
                                inventoryImageIdsLoaded.Add(inventory.ImageId);

                                TabParent.AddInventoryImage(imageData);
                            }
                        }
                    }
                }
            }
        }

        public void Help_ArrangementPage_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(HelpPage)))
            {
                TaskAwaiter t = Navigation.PushAsync(new HelpPage("ArrangementPage")).GetAwaiter();
            }
        }

        private void Products_Clicked(object sender, EventArgs e)
        {
            //in the case where an arrangement is being added to a work order "on the fly"
            //there WILL be anothere ArrangementFilterPage on the nav stack - so turn the button
            //off instead

            Products.IsEnabled = false;
            Navigation.PushAsync(new ArrangementFilterPage(this, false));

        }

        private void GiftCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;

            if(cb != null)
            {
                GiftMessageLabel.IsEnabled = cb.IsChecked;
                GiftMessageLabel.IsVisible = cb.IsChecked;

                GiftMessage.IsEnabled = cb.IsChecked;
                GiftMessage.IsVisible = cb.IsChecked;
            }
        }

        private bool NotInInventoryItemIsinList(NotInInventoryDTO dto)
        {
            return notInInventoryList.Where(a => a.NotInInventoryName == dto.NotInInventoryName &&
                a.NotInInventorySize == dto.NotInInventorySize && a.NotInInventoryPrice == dto.NotInInventoryPrice).Any();
        }
    }
}