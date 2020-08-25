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

        List<PlantInventoryDTO> plants = new List<PlantInventoryDTO>();

        ObservableCollection<PlantInventoryDTO> list3 = new ObservableCollection<PlantInventoryDTO>();

        public PlantPage()
        {
            InitializeComponent();

            List<PlantTypeDTO> plantTypes = GetPlantTypes();

            ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();

            foreach (PlantTypeDTO code in plantTypes)
            {
                list1.Add(new KeyValuePair<long, string>(code.PlantTypeId, code.PlantTypeName));
            }

            PlantType.ItemsSource = list1;

            List<string> sizes = ((App)App.Current).GetSizeByInventoryType(1);

            ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

            long index = 1;
            foreach (string size in sizes)
            {
                list2.Add(new KeyValuePair<long, string>(index++, size));
            }

            PlantSize.ItemsSource = list2;

            PlantType.SelectedIndexChanged += PlantType_SelectedIndexChanged;

            PlantName.SelectedIndexChanged += PlantName_SelectedIndexChanged;

            PlantSize.SelectedIndexChanged += PlantSize_SelectedIndexChanged;

            plants = ((App)App.Current).GetPlants().PlantInventoryList;

            //foreach(PlantInventoryDTO p in plants)
            //{
            //    list3.Add(p);
            //}

            //plantListView.ItemsSource = list3;
        }


        public List<PlantTypeDTO> GetPlantTypes()
        {
            List<PlantTypeDTO> plantTypes = new List<PlantTypeDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse = client.GetAsync("api/Login/GetPlantTypes").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetPlantTypeResponse response = JsonConvert.DeserializeObject<GetPlantTypeResponse>(strData);
                    plantTypes = response.PlantTypes;
                }
                else
                {
                   // MessageBox.Show("There was an error retreiving plant types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetPlantTypes", ex);
                ((App)App.Current).LogError(ex2.Message, String.Empty);
            }
            return plantTypes;
        }

        public GetPlantResponse GetPlantsByType(long plantTypeId)
        {
            GetPlantResponse plants = new GetPlantResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetPlantsByType?plantTypeId=" + plantTypeId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    plants = JsonConvert.DeserializeObject<GetPlantResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plants");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetPlantsByType", ex);
                ((App)App.Current).LogError(ex2.Message, String.Empty);
            }

            return plants;
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

            long selectedValue = ((KeyValuePair<long, string>)PlantName.SelectedItem).Key;
            string selectedPlantName = ((KeyValuePair<long, string>)PlantName.SelectedItem).Value;

            ObservableCollection<PlantInventoryDTO> pDTO = new ObservableCollection<PlantInventoryDTO>();

            foreach (PlantInventoryDTO p in plants.Where(a => a.Plant.PlantName == selectedPlantName))
            {
                pDTO.Add(p);
            }

            plantListView.ItemsSource = pDTO;
        }

        private void PlantType_SelectedIndexChanged(object sender, EventArgs e)
        {
            PlantSize.SelectedIndex = -1;

            long selectedValue = ((KeyValuePair<long, string>)PlantType.SelectedItem).Key;

            GetPlantResponse response = GetPlantsByType(selectedValue);

            ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

            ObservableCollection<PlantInventoryDTO> pDTO = new ObservableCollection<PlantInventoryDTO>();

            foreach (PlantInventoryDTO p in plants.Where(a => a.Plant.PlantTypeId == selectedValue))
            {
                list2.Add(new KeyValuePair<long, string>(p.Plant.PlantId, p.Plant.PlantName));

                pDTO.Add(p);
            }

            PlantName.ItemsSource = list2;

            plantListView.ItemsSource = pDTO;
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

                    ServiceCodeDTO serviceCode = ((App)App.Current).GetServiceCodeById(plant.Inventory.ServiceCodeId);

                    string price = string.Empty;
                    if(serviceCode.ServiceCodeId > 0)
                    {
                        price = (serviceCode.Price.HasValue ? serviceCode.Price.Value.ToString("C2", CultureInfo.CurrentCulture) : String.Empty);
                    }

                    PopupImagePage popup = new PopupImagePage(img, price);

                    Navigation.PushPopupAsync(popup);

                    if(plantImageId == ((App)App.Current).MissingImageId)
                    {
                        MessagingCenter.Send<PlantInventoryDTO>(plant, "PlantMissingImage");
                    }
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

        private void Help_PlantsPage_Clicked(object sender, EventArgs e)
        {
            TaskAwaiter t = Navigation.PushAsync(new HelpPage("PlantsPage")).GetAwaiter();
        }
    }
}