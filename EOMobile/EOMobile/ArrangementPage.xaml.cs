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
        List<ArrangementInventoryDTO> arrangementInventoryList = new List<ArrangementInventoryDTO>();
        List<GetSimpleArrangementResponse> arrangementList = new List<GetSimpleArrangementResponse>();

        ObservableCollection<ArrangementInventoryDTO> arrangementInventoryListOC = new ObservableCollection<ArrangementInventoryDTO>();
        ObservableCollection<GetSimpleArrangementResponse> arrangementListOC = new ObservableCollection<GetSimpleArrangementResponse>();

        TabbedArrangementPage TabParent = null;

        List<long> inventoryImageIdsLoaded = new List<long>();

        long? customerContainerId = null;

        long NotInInventoryTempId = 0;

        List<NotInInventoryDTO> notInInventoryList = new List<NotInInventoryDTO>();
        
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

            ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();
            list1.Add(new KeyValuePair<long, string>(1, "Vicky"));
            list1.Add(new KeyValuePair<long, string>(2, "Marguerita"));

            Designer.ItemsSource = list1;

            ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();
            list2.Add(new KeyValuePair<long, string>(1, "180"));
            list2.Add(new KeyValuePair<long, string>(2, "360"));

            Style.ItemsSource = list2;


            ObservableCollection<KeyValuePair<long, string>> list3 = new ObservableCollection<KeyValuePair<long, string>>();

            list3.Add(new KeyValuePair<long, string>(1, "New container"));

            if (tabParent.Customer != null)
            {
                list3.Add(new KeyValuePair<long, string>(2, "Customer container at EO"));
                list3.Add(new KeyValuePair<long, string>(3, "Customer container at customer site")); //means use liner
            }

            Container.ItemsSource = list3;

            EnableCustomerContainerSecondaryControls(false);

            TabParent = tabParent;

            GiftCheckBox.IsChecked = false;

            if (TabParent.CurrentArrangement != null)
            {
                LoadWorkOrderArrangement();
            }

            ArrangementListView.ItemSelected += ArrangementListView_ItemSelected;
        }

        private void LoadWorkOrderArrangement()
        {
            //load data passed from parent
            Name.Text = TabParent.CurrentArrangement.Arrangement.ArrangementName;

            Location.Text = TabParent.CurrentArrangement.Arrangement.LocationName;

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
                if (kvp.Key == TabParent.CurrentArrangement.Arrangement.Container)
                {
                    Container.SelectedIndex = index;
                    break;
                }
                index++;
            }

            arrangementInventoryList = TabParent.CurrentArrangement.ArrangementInventory;

            foreach(NotInInventoryDTO nii in TabParent.CurrentArrangement.NotInInventory)
            {
                arrangementInventoryList.Add(new ArrangementInventoryDTO
                {
                    ArrangementId = TabParent.CurrentArrangement.Arrangement.ArrangementId,
                    ArrangementInventoryName = nii.NotInInventoryName,
                    Quantity = nii.NotInInventoryQuantity,
                    Size = nii.NotInInventorySize,
                });

                notInInventoryList.Add(nii);
            }

            foreach (ArrangementInventoryDTO a in arrangementInventoryList)
            {
                arrangementInventoryListOC.Add(a);
            }

            ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;
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
            ArrangementInventoryDTO searchedForInventory = ((App)App.Current).searchedForArrangementInventory;

            if (searchedForInventory != null && searchedForInventory.InventoryId != 0)
            {
                if (!arrangementInventoryList.Contains(searchedForInventory))
                {
                    arrangementInventoryListOC.Clear();

                    searchedForInventory.Quantity = 1;

                    arrangementInventoryList.Add(searchedForInventory);

                    foreach (ArrangementInventoryDTO a in arrangementInventoryList)
                    {
                        arrangementInventoryListOC.Add(a);
                    }

                    ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;

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
            arrangementInventoryListOC.Clear();
            ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;
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
                    AddArrangementRequest request = new AddArrangementRequest();
                    request.Arrangement = new ArrangementDTO();

                    request.Arrangement.ArrangementId = TabParent.CurrentArrangement != null ? TabParent.CurrentArrangement.Arrangement.ArrangementId : 0;

                    request.Arrangement.ArrangementName = Name.Text;
                    request.Arrangement.DesignerName = Designer.SelectedItem != null ? ((KeyValuePair<long, string>)Designer.SelectedItem).Value : String.Empty;
                    request.Arrangement._180or360 = Style.SelectedItem != null ? (int)((KeyValuePair<long, string>)Style.SelectedItem).Key : 1;
                    request.Arrangement.Container = Container.SelectedItem != null ? (int)((KeyValuePair<long, string>)Style.SelectedItem).Key : 1;   //1 = new container (db default)
                    request.Arrangement.LocationName = Location.Text;
                    request.Arrangement.UpdateDate = DateTime.Now;
                    request.ArrangementInventory = arrangementInventoryList.Where(a => a.InventoryId != 0).ToList();  // "Not In Inventory" items may have been added to the display list
                    request.Arrangement.IsGift = GiftCheckBox.IsChecked ? 1 : 0;
                    request.Arrangement.GiftMessage = GiftMessage.Text;

                    Random r = new Random();
                    long tempArrangementId = r.Next(1, 100);

                    foreach(NotInInventoryDTO dto in notInInventoryList)
                    {
                        //group them for the work order
                        if(!dto.ArrangementId.HasValue || dto.ArrangementId == 0)
                        {
                            dto.ArrangementId = tempArrangementId;
                        }
                    }

                    request.NotInInventory = notInInventoryList;

                    request.Inventory = new InventoryDTO()
                    {
                        InventoryName = String.IsNullOrEmpty(Name.Text) ? "Arrangement_" + Convert.ToString(tempArrangementId) : Name.Text,
                        InventoryTypeId = 5,
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
                    foreach (ArrangementInventoryDTO dto in arrangementInventoryList)
                    {
                        if (dto.ArrangementId != 0)
                        {
                            arrangementId = dto.ArrangementId;
                            break;
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
                        request.ArrangementInventory = arrangementInventoryList;

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

                        UpdateArrangementRequest request = new UpdateArrangementRequest();
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

                        request.ArrangementItems = arrangementInventoryList;

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
                //Command parameter is InventoryId
                if (button.CommandParameter != null)
                {
                    long deleteItemId = (long)(button).CommandParameter;
                    ArrangementInventoryDTO dto = arrangementInventoryList.Where(a => a.InventoryId == deleteItemId).FirstOrDefault();
                    arrangementInventoryList.Remove(dto);
                    arrangementInventoryListOC.Remove(dto);
                    ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;
                }
            }
        }

        private void Quantity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Xamarin.Forms.Entry entry = sender as Xamarin.Forms.Entry;

            if (entry != null)
            {
                string strQty = entry.Text;

                if (!String.IsNullOrEmpty(strQty))
                {
                    int qty = Convert.ToInt32(strQty);
                }
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

                arrangementInventoryListOC.Clear();

                foreach (ArrangementInventoryDTO a in arrangementInventoryList)
                {
                    arrangementInventoryListOC.Add(a);
                }

                ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;
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
                    ArrangementInventoryDTO inventory = arrangementInventoryList.Where(a => a.InventoryId == inventoryId).FirstOrDefault();

                    if (inventory.ImageId != 0)
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

        private void AddItemNotInInventory_Clicked(object sender, EventArgs e)
        {
            String msg = String.Empty;
            if (NotInInventoryName.Text == String.Empty)
            {
                msg += "Please add a name for the item not in inventory. \n";
            }

            if (NotInInventorySize.Text == String.Empty)
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

            if (msg != String.Empty)
            {
                DisplayAlert("Error", msg, "Ok");
            }
            else
            {
                //add this item to the list
                NotInInventoryDTO dto = new NotInInventoryDTO();

                dto.WorkOrderId = 0;
                dto.ArrangementId = 0;
                dto.NotInInventoryName = NotInInventoryName.Text;
                dto.NotInInventoryQuantity = Convert.ToInt32(NotInInventoryQuantity.Text);
                dto.NotInInventorySize = NotInInventorySize.Text;
                dto.NotInInventoryPrice = Convert.ToDecimal(NotInInventoryPrice.Text);

                if (!NotInInventoryItemIsinList(dto))
                {
                    NotInInventoryName.Text = String.Empty;
                    NotInInventoryQuantity.Text = String.Empty;
                    NotInInventorySize.Text = String.Empty;
                    NotInInventoryPrice.Text = String.Empty;

                    notInInventoryList.Add(dto);

                    arrangementInventoryList.Add(new ArrangementInventoryDTO()
                    {
                        ArrangementId = 0,
                        ArrangementInventoryName = dto.NotInInventoryName,
                        InventoryId = 0,
                        InventoryTypeId = 0,
                        Quantity = dto.NotInInventoryQuantity,
                        Size = dto.NotInInventorySize,
                    });


                    arrangementInventoryListOC.Clear();

                    foreach (ArrangementInventoryDTO a in arrangementInventoryList)
                    {
                        arrangementInventoryListOC.Add(a);
                    }

                    ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;
                }
            }
        }

        private bool NotInInventoryItemIsinList(NotInInventoryDTO dto)
        {
            return notInInventoryList.Where(a => a.NotInInventoryName == dto.NotInInventoryName &&
                a.NotInInventorySize == dto.NotInInventorySize && a.NotInInventoryPrice == dto.NotInInventoryPrice).Any();
        }
    }
}