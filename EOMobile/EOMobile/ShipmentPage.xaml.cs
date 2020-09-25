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

    /// <summary>
    /// Shipments allow one or multiple images to be saved PER line item.
    /// Images that have been taken PER line item will be shown when an 
    /// individual line item is selected - per line item image collections need 
    /// to be saved locally before the shipment, as a data collection, is saved to the 
    /// database. Previously saved line item images need to be loaded to local storage
    /// so that those images can be shown when a shipment is loaded in an edit mode
    /// or in a report mode. The "selected" item index must be saved locally, since that 
    /// state, if set, will be lost when navigating to the camera page and back. 
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShipmentPage : EOBasePage
    {
        private List<VendorDTO> vendorList;
        ShipmentInventoryDTO shipment = new ShipmentInventoryDTO();
        List<ShipmentInventoryItemDTO> shipmentInventoryList = new List<ShipmentInventoryItemDTO>();
        private List<InventoryTypeDTO> inventoryTypeList = new List<InventoryTypeDTO>();

        List<UserDTO> users = new List<UserDTO>();
        List<KeyValuePair<long, string>> employeeDDL = new List<KeyValuePair<long, string>>();
        TabbedShipmentPage TabParent = null;

        long selectedInventoryItemId = -1;

        public string User
        {
            get { return ((App)App.Current).User; }
            set { ((App)App.Current).User = value; }
        }

        public string Pwd
        {
            get { return ((App)App.Current).Pwd; }
            set { ((App)App.Current).Pwd = value; }
        }

        public ShipmentPage(TabbedShipmentPage tabParent)
        {
            Initialize(tabParent);

            MessagingCenter.Subscribe<EOImgData>(this, "PictureTaken", (arg) =>
            {
                PictureTaken(arg);
            });
        }

        public ShipmentPage(TabbedShipmentPage tabParent, long shipmentId) : this(tabParent)
        {
            ((App)(App.Current)).GetShipment(shipmentId).ContinueWith(a => LoadShipment(a.Result));
        }

        private void Initialize(TabbedShipmentPage tabParent)
        {
            InitializeComponent();

            TabParent = tabParent;

            ((App)App.Current).GetUsers().ContinueWith(a => LoadUsers(a.Result));

            ((App)(App.Current)).GetVendors(new GetPersonRequest()).ContinueWith(a => LoadVendors(a.Result));
        }

        private void LoadUsers(GetUserResponse userResponse)
        {
            foreach (UserDTO user in userResponse.Users)
            {
                employeeDDL.Add(new KeyValuePair<long, string>(user.UserId, user.UserName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                Receiver.ItemsSource = employeeDDL;
            });
        }

        private void LoadVendors(GetVendorResponse response)
        {
            vendorList = response.VendorList;

            ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();
            foreach (VendorDTO v in vendorList)
            {
                list1.Add(new KeyValuePair<long, string>(v.VendorId, v.VendorName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                Vendor.ItemsSource = list1;
            });
        }

        private void LoadShipment(ShipmentInventoryDTO shipmentDTO)
        {
            shipment = shipmentDTO;

            shipmentInventoryList.Clear();

            foreach (ShipmentInventoryMapDTO map in shipment.ShipmentInventoryMap)
            {

                shipmentInventoryList.Add(new ShipmentInventoryItemDTO()
                {
                    InventoryId = map.InventoryId,
                    InventoryName = map.InventoryName,
                    Quantity = map.Quantity,
                    ShipmentId = map.ShipmentId,
                    imageMap = map.ShipmentInventoryImageMap
                });
            }

            ObservableCollection<ShipmentInventoryItemDTO> list1 = new ObservableCollection<ShipmentInventoryItemDTO>();

            foreach (ShipmentInventoryItemDTO wo in shipmentInventoryList)
            {
                list1.Add(wo);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                Vendor.SelectedIndex = ((App)App.Current).GetPickerIndex(Vendor, shipment.Shipment.VendorId);

                Receiver.SelectedIndex = ((App)App.Current).GetPickerIndex(Receiver, shipment.Shipment.ReceiverId);

                ShipmentItemsListView.ItemsSource = list1;
            });
        }

        //called by TabbedShipmentPage to load images to the Image page
        //done once at init shipment page is created first to the parent has to call this
        //as part of initialization
        public List<EOImgData> LoadImageData()
        {
            List<EOImgData> imgData = new List<EOImgData>();

            foreach(ShipmentInventoryItemDTO sii in shipmentInventoryList)
            {
                foreach(ShipmentInventoryImageMapDTO map in sii.imageMap)
                {
                    imgData.Add(new EOImgData(map.ImageId, map.ImageData));
                }
            }

            return imgData;
        }

        public void PictureTaken(EOImgData imgData)
        {
            ShipmentInventoryItemDTO inventoryItem =
                shipmentInventoryList.Where(a => a.InventoryId == selectedInventoryItemId).FirstOrDefault();

            if (inventoryItem != null && inventoryItem.InventoryId != 0)
            {
                inventoryItem.imageMap.Add(new ShipmentInventoryImageMapDTO()
                {
                    ImageData = imgData.imgData
                });
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ShipmentInventoryItemDTO searchedForShipmentInventory = ((App)App.Current).searchedForShipmentInventory;

            if (searchedForShipmentInventory != null && searchedForShipmentInventory.InventoryId != 0)
            {
                selectedInventoryItemId = searchedForShipmentInventory.InventoryId;

                if (!shipmentInventoryList.Contains(searchedForShipmentInventory))
                {
                    shipmentInventoryList.Add(searchedForShipmentInventory);
                    ObservableCollection<ShipmentInventoryItemDTO> list1 = new ObservableCollection<ShipmentInventoryItemDTO>();

                    foreach (ShipmentInventoryItemDTO so in shipmentInventoryList)
                    {
                        list1.Add(so);
                    }

                    ShipmentItemsListView.ItemsSource = list1;

                    ((App)App.Current).searchedForShipmentInventory = null;
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
        public void OnInventorySearchClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(ArrangementFilterPage)))
            {
                Navigation.PushAsync(new ArrangementFilterPage(this));
            }
        }

        public void OnShipmentSaveClicked(object sender, EventArgs e)
        {
            if (shipmentInventoryList.Count > 0)
            {
                AddShipment();
            }
        }

        public void AddShipment()
        {
            AddShipmentRequest addShipmentRequest = new AddShipmentRequest();

            ShipmentDTO dto = new ShipmentDTO()
            {
                VendorId = ((KeyValuePair<long, string>)this.Vendor.SelectedItem).Key,
                VendorName = ((KeyValuePair<long, string>)this.Vendor.SelectedItem).Value,
                Receiver = ((KeyValuePair<long, string>)this.Receiver.SelectedItem).Value,
                ReceiverId = ((KeyValuePair<long, string>)this.Receiver.SelectedItem).Key,
                ShipmentDate = DateTime.Now,
                //Comments = this.Comments.Text
            };

            List<ShipmentInventoryMapDTO> shipmentInventoryMap = new List<ShipmentInventoryMapDTO>();

            foreach (ShipmentInventoryItemDTO woii in shipmentInventoryList)
            {
                ShipmentInventoryMapDTO sim = new ShipmentInventoryMapDTO()
                {
                    InventoryId = woii.InventoryId,
                    InventoryName = woii.InventoryName,
                    Quantity = woii.Quantity
                };

                sim.ShipmentInventoryImageMap = woii.imageMap;

                shipmentInventoryMap.Add(sim);
            }

            addShipmentRequest.ShipmentDTO = dto;
            addShipmentRequest.ShipmentInventoryMap = shipmentInventoryMap;

            ((App)App.Current).AddShipment(addShipmentRequest);

            this.Vendor.SelectedIndex = -1;
            this.Receiver.SelectedIndex = -1;
            this.shipmentInventoryList.Clear();
            this.ShipmentItemsListView.ItemsSource = null;
            ((App)App.Current).ClearImageData();
        }

        public void OnDeleteShipmentItem(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                long itemId = Int64.Parse(button.CommandParameter.ToString());

                ShipmentInventoryItemDTO sel = shipmentInventoryList.Where(a => a.InventoryId == itemId).FirstOrDefault();

                if (sel.InventoryId != 0)
                {
                    shipmentInventoryList.Remove(sel);

                    ObservableCollection<ShipmentInventoryItemDTO> list1 = new ObservableCollection<ShipmentInventoryItemDTO>();

                    foreach (ShipmentInventoryItemDTO wo in shipmentInventoryList)
                    {
                        list1.Add(wo);
                    }

                    ShipmentItemsListView.ItemsSource = list1;

                    TabParent.ClearShipmentImages();
                }
            }
        }

        private void ImagePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            Picker p = sender as Picker;

            if (p != null)
            {
                ShipmentInventoryItemDTO inventoryItem = p.Parent.BindingContext as ShipmentInventoryItemDTO;
                                
                if (p.SelectedIndex == 0)
                {
                    StartCamera();
                }
                else if (p.SelectedIndex == 1)
                {
                    //view images
                }
            }
        }

        private void AddShipmentInventory_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(ArrangementFilterPage)))
            {
                ((App)App.Current).ClearImageData();

                TabParent.ClearShipmentImages();

                Navigation.PushAsync(new ArrangementFilterPage(this));
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

        private void ShipmentItemsListView_ChildRemoved(object sender, ElementEventArgs e)
        {
            int debug = 1;

            ((App)App.Current).ClearImageData();
            ((App)App.Current).ClearImageDataList();
        }

        private void ShipmentItemsListView_Unfocused(object sender, FocusEventArgs e)
        {
            int debug = 1;

            //get current image data, if any and store in inventoryItem object
            ((App)App.Current).ClearImageData();
            ((App)App.Current).ClearImageDataList();
        }

        private void ShipmentItemsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            TabParent.ClearShipmentImages();
            ((App)App.Current).ClearImageData();

            ShipmentInventoryItemDTO inventoryItem = ShipmentItemsListView.SelectedItem as ShipmentInventoryItemDTO;

            if(inventoryItem != null)
            {
                selectedInventoryItemId = inventoryItem.InventoryId;

                List<EOImgData> imageData = new List<EOImgData>();

                foreach(ShipmentInventoryImageMapDTO imageMap in inventoryItem.imageMap)
                {
                   imageData.Add(new EOImgData(imageMap.ImageId, imageMap.ImageData));
                }

                TabParent.AddShipmentImages(imageData);
            }
        }

        private void Help_ShipmentPage_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(HelpPage)))
            {
                TaskAwaiter t = Navigation.PushAsync(new HelpPage("ShipmentPage")).GetAwaiter();
            }
        }
    }
}