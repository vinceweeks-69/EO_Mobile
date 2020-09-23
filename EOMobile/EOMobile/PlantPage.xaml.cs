using Newtonsoft.Json;
using Rg.Plugins.Popup.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
    public partial class PlantPage : EOBasePage
    {
        List<PlantInventoryDTO> plants = new List<PlantInventoryDTO>();

        ObservableCollection<PlantInventoryDTO> list3 = new ObservableCollection<PlantInventoryDTO>();

        public PlantPage()
        {
            InitializeComponent();

            LoadTypes();
        }

        public async void LoadTypes()
        {
            GenericGetRequest request = new GenericGetRequest("GetPlantTypes", String.Empty, 0);

            ((App)App.Current).GetRequest<GetPlantTypeResponse>(request).ContinueWith(a => ShowTypes(a.Result));
        }

        public void ShowTypes(GetPlantTypeResponse response)
        {
            ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();

            foreach (PlantTypeDTO code in response.PlantTypes)
            {
                list1.Add(new KeyValuePair<long, string>(code.PlantTypeId, code.PlantTypeName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                PlantType.ItemsSource = list1;

                PlantType.SelectedIndexChanged += PlantType_SelectedIndexChanged;
            });

            LoadSizes();
        }

        public async void LoadSizes()
        {
            GenericGetRequest request = new GenericGetRequest("GetSizeByInventoryType", "inventoryTypeId", 1);

            ((App)App.Current).GetSizeByInventoryType(request).ContinueWith(a => ShowSizes(a.Result));
        }

        public void ShowSizes(GetSizeResponse response)
        {
            ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

            long index = 1;
            foreach (string size in response.Sizes)
            {
                list2.Add(new KeyValuePair<long, string>(index++, size));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                PlantSize.ItemsSource = list2;

                PlantSize.SelectedIndexChanged += PlantSize_SelectedIndexChanged;
            });

            LoadPlants();
        }

        public async void LoadPlants()
        {
            GenericGetRequest request = new GenericGetRequest("GetPlants", String.Empty, 0);
            ((App)App.Current).GetRequest<GetPlantResponse>(request).ContinueWith(a => ShowPlants(a.Result));
        }

        public void ShowPlants(GetPlantResponse response)
        {
            plants = response.PlantInventoryList;

            foreach(PlantInventoryDTO p in plants)
            {
                list3.Add(p);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                plantListView.ItemsSource = list3;

                PlantName.SelectedIndexChanged += PlantName_SelectedIndexChanged;
            });
        }

        private void PlantSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeyValuePair<long, string> selectedItem = (KeyValuePair<long, string>)PlantSize.SelectedItem;

            if (!String.IsNullOrEmpty(selectedItem.Value))
            {
                long selectedValue = ((KeyValuePair<long, string>)PlantSize.SelectedItem).Key;
                string selectedPlantSize = ((KeyValuePair<long, string>)PlantSize.SelectedItem).Value;

                ObservableCollection<PlantInventoryDTO> pDTO = new ObservableCollection<PlantInventoryDTO>();

                foreach (PlantInventoryDTO p in plants.Where(a => a.Plant.PlantSize == selectedPlantSize))
                {
                    pDTO.Add(p);
                }

                plantListView.ItemsSource = pDTO;
            }
        }
        
        private void PlantName_SelectedIndexChanged(object sender, EventArgs e)
        {
            PlantSize.SelectedIndex = -1;

            if (PlantName.SelectedItem != null)
            {
                string selectedPlantName = ((KeyValuePair<long, string>)PlantName.SelectedItem).Value;

                ObservableCollection<PlantInventoryDTO> pDTO = new ObservableCollection<PlantInventoryDTO>();

                foreach (PlantInventoryDTO p in plants.Where(a => a.Plant.PlantName == selectedPlantName))
                {
                    pDTO.Add(p);
                }

                plantListView.ItemsSource = pDTO;
            }
        }

        private void PlantType_SelectedIndexChanged(object sender, EventArgs e)
        {
            PlantName.SelectedIndex = -1;
            PlantSize.SelectedIndex = -1;

            if (PlantType.SelectedItem != null)
            {
                long selectedValue = ((KeyValuePair<long, string>)PlantType.SelectedItem).Key;

                ((App)App.Current).GetPlantsByType(selectedValue).ContinueWith(a => ShowSelectedPlantTypes(a.Result, selectedValue));
            }
        }

        private void ShowSelectedPlantTypes(GetPlantResponse response, long selectedValue)
        {
            plants = response.PlantInventoryList;

            ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

            ObservableCollection<PlantInventoryDTO> pDTO = new ObservableCollection<PlantInventoryDTO>();

            foreach (PlantInventoryDTO p in plants.Where(a => a.Plant.PlantTypeId == selectedValue))
            {
                list2.Add(new KeyValuePair<long, string>(p.Plant.PlantId, p.Plant.PlantName));

                pDTO.Add(p);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                PlantName.ItemsSource = list2;

                plantListView.ItemsSource = pDTO;
            });
        }

        private void ViewImage_Clicked(object sender, EventArgs e)
        {
            IReadOnlyList<Rg.Plugins.Popup.Pages.PopupPage> popupStack = Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack;

            //One at a time, please
            if(popupStack != null && popupStack.Count > 0)
            {
                return;
            }

            Button b = sender as Button;
            b.IsEnabled = false;

            try
            {
               PlantInventoryDTO plant = (PlantInventoryDTO)((Button)sender).BindingContext;

                if (plant != null)
                {
                    long plantImageId = ((App)App.Current).MissingImageId;
                    if (plant.ImageId != 0)
                    {
                        plantImageId = plant.ImageId;
                    }

                    EOImgData img = ((App)App.Current).GetImage(plantImageId);

                    if (plantImageId == ((App)App.Current).MissingImageId)
                    {
                        MessagingCenter.Send<PlantInventoryDTO>(plant, "PlantMissingImage");
                    }

                    ((App)App.Current).GetServiceCodeById(plant.Inventory.ServiceCodeId).ContinueWith(a => ShowImage(img,a.Result));
                }
            }
            catch(Exception ex)
            {

            }
            finally
            {
                b.IsEnabled = true;
            }
        }

        private void ShowImage(EOImgData img, ServiceCodeDTO serviceCode)
        {
            string price = string.Empty;
            if (serviceCode.ServiceCodeId > 0)
            {
                price = (serviceCode.Price.HasValue ? serviceCode.Price.Value.ToString("C2", CultureInfo.CurrentCulture) : String.Empty);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                PopupImagePage popup = new PopupImagePage(img, price);

                Navigation.PushPopupAsync(popup);
            });
        }

        private void Help_PlantsPage_Clicked(object sender, EventArgs e)
        {
            TaskAwaiter t = Navigation.PushAsync(new HelpPage("PlantsPage")).GetAwaiter();
        }
    }
}