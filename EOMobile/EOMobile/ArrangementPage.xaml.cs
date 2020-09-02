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
            list3.Add(new KeyValuePair<long, string>(1, "Customer container at EO"));  
            list3.Add(new KeyValuePair<long, string>(2, "Customer container at customer site")); //means use liner
            list3.Add(new KeyValuePair<long, string>(3, "New container"));

            Container.ItemsSource = list3;

            Container.SelectedIndexChanged += Container_SelectedIndexChanged;

            TabParent = tabParent;
        }

        private void Container_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if container type is "Customer container at EO" pick from  customer containers on site

            //if container type is "Customer container at customer site" means use liner

            //if container type is "New container" pick container from inventory
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

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
        }

        private void SetWorkOrderSalesData()
        {
            //GetWorkOrderSalesDetailResponse response = GetWorkOrderDetail();

            //SubTotal.Text = response.SubTotal.ToString("C", CultureInfo.CurrentCulture);
            //Tax.Text = response.Tax.ToString("C", CultureInfo.CurrentCulture);
            //Total.Text = response.Total.ToString("C", CultureInfo.CurrentCulture);
        }

        public void OnInventorySearchClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ArrangementFilterPage(this, false));
        }

        public void OnSearchArrangementsClicked(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(Name.Text))
            {
                arrangementList.Clear();
                arrangementListOC.Clear();

                arrangementList = ((App)App.Current).GetArrangements(Name.Text);

                foreach (GetSimpleArrangementResponse ar in arrangementList)
                {
                    arrangementListOC.Add(ar);
                }

                ArrangementListView.ItemsSource = arrangementListOC;
            }
            else
            {
                DisplayAlert("Error", "To search arrangements, enter an arrangement name", "Ok");
            }
        }

        public void OnClearArrangementsClicked(object sender, EventArgs e)
        {
            ClearArrangements();
        }

        public void ClearArrangements()
        {
            Name.Text = String.Empty;
            arrangementList.Clear();
            arrangementListOC.Clear();
            arrangementInventoryList.Clear();
            arrangementInventoryListOC.Clear();
            ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;
            inventoryImageIdsLoaded.Clear();
            TabParent.ClearArrangementImages();
        }

        public bool ValidateArrangement(ref string validationMessage)
        {
            if(arrangementInventoryList.Count < 2)
            {
                validationMessage += "Please add at least one container or container value and one inventory item. \n";
            }
            else
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

                    if (containerVal == 3) // "new container"
                    {
                        //check inventory for an inventory item of type container
                        if(!arrangementInventoryList.Where(a => a.Type.Equals("Containers")).Any())
                        {
                            validationMessage += "Please pick a Container. \n";
                        }
                        else
                        {
                            if (arrangementInventoryList.Where(a => a.Type.Equals("Containers")).Count() > 1)
                            {
                                validationMessage += "An arrangement can have only one container. \n";
                            }
                        }
                    }
                    else
                    {
                        //make sure CustomerContainerId is not null
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
                    request.Arrangement.ArrangementName = Name.Text;
                    request.Arrangement.DesignerName = Designer.SelectedItem != null ? ((KeyValuePair<long,string>)Designer.SelectedItem).Value : String.Empty;
                    request.Arrangement._180or360 = Style.SelectedItem != null ? (int)((KeyValuePair<long, string>)Style.SelectedItem).Key : 1;
                    request.Arrangement.Container = Container.SelectedItem != null ? (int)((KeyValuePair<long, string>)Style.SelectedItem).Key : 3;   //3 = new container (db default)
                    request.Arrangement.LocationName = Location.Text;
                    request.Arrangement.UpdateDate = DateTime.Now;
                    request.ArrangementInventory = arrangementInventoryList;

                    request.Inventory = new InventoryDTO()
                    {
                        InventoryName = Name.Text,
                        InventoryTypeId = 5,
                    };

                    MessagingCenter.Send<AddArrangementRequest>(request, "AddArrangementToWorkOrder");

                    PopToPage("TabbedWorkOrderPage");

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

                        request.Arrangement.UpdateDate = DateTime.Now;
                        request.ArrangementInventory = arrangementInventoryList;

                        request.Inventory = new InventoryDTO()
                        {
                            InventoryName = Name.Text,
                            InventoryTypeId = 5,
                        };

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
                        }
                    }

                    ClearArrangements();
                }
            }
            catch(Exception ex)
            {

            }
        }

        //public async void StartCamera()
        //{
        //    try
        //    {
        //        var action = await DisplayActionSheet("Add Photo", "Cancel", null, "Choose Existing", "Take Photo");

        //        if (action == "Choose Existing")
        //        {
        //            Device.BeginInvokeOnMainThread(() =>
        //            {
        //                var fileName = ((App)App.Current).SetImageFileName();
        //                DependencyService.Get<ICameraInterface>().LaunchGallery(FileFormatEnum.JPEG, fileName);
        //            });
        //        }
        //        else if (action == "Take Photo")
        //        {
        //            Device.BeginInvokeOnMainThread(() =>
        //            {
        //                var fileName = ((App)App.Current).SetImageFileName();
        //                DependencyService.Get<ICameraInterface>().LaunchCamera(FileFormatEnum.JPEG, fileName);
        //            });
        //        }
        //    }
        //    catch(Exception ex)
        //    {

        //    }
        //}

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
            GetArrangementResponse response = ((App)App.Current).GetArrangement(item.Arrangement.ArrangementId);

            ClearArrangements();

            Name.Text = response.Arrangement.ArrangementName;

            arrangementList.Add(item);

            arrangementInventoryList = response.ArrangementList;

            ObservableCollection<ArrangementInventoryDTO> list1 = new ObservableCollection<ArrangementInventoryDTO>();

            foreach (ArrangementInventoryDTO a in arrangementInventoryList)
            {
                arrangementInventoryListOC.Add(a);
            }

            ArrangementItemsListView.ItemsSource = arrangementInventoryListOC;

            TabParent.LoadArrangmentImages(response.Images);

            lv.SelectedItem = null;
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
            TaskAwaiter t = Navigation.PushAsync(new HelpPage("ArrangementPage")).GetAwaiter();
        }
    }
}