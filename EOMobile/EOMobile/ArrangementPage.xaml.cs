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

            TabParent = tabParent;
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
            Navigation.PushModalAsync(new ArrangementFilterPage(this));
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
        public void OnSaveArrangementsClicked(object sender, EventArgs e)
        {
            try
            {
                long arrangementId = 0;  //(long)((Button)sender).CommandParameter;

                if (!String.IsNullOrEmpty(Name.Text) && arrangementInventoryList.Count > 0)
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
                else
                {
                    DisplayAlert("Error", "Please enter an arrangement name and add at least one inventory item.", "OK");
                }
            }
            catch(Exception ex)
            {

            }
        }

        public async void StartCamera()
        {
            try
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