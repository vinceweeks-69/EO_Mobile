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
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FoliagePage : EOBasePage
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

        List<FoliageInventoryDTO> foliage = new List<FoliageInventoryDTO>();

        ObservableCollection<FoliageInventoryDTO> list2 = new ObservableCollection<FoliageInventoryDTO>();

        public FoliagePage()
        {
            InitializeComponent();

            List<FoliageTypeDTO> foliageTypes = GetFoliageTypes();

            ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();

            foreach (FoliageTypeDTO code in foliageTypes)
            {
                list1.Add(new KeyValuePair<long, string>(code.FoliageTypeId, code.FoliageTypeName));
            }

            FoliageType.ItemsSource = list1;

            foreach(FoliageInventoryDTO f in GetFoliage().FoliageInventoryList)
            {
                list2.Add(f);
            }

            foliageListView.ItemsSource = list2;
        }

        public GetFoliageResponse GetFoliage()
        {
            GetFoliageResponse response = new GetFoliageResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetFoliage").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    response = JsonConvert.DeserializeObject<GetFoliageResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plant types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetFoliage", ex);
                ((App)App.Current).LogError(ex2.Message, String.Empty);
            }

            return response;
        }

        public List<FoliageTypeDTO> GetFoliageTypes()
        {
            List<FoliageTypeDTO> foliageTypes = new List<FoliageTypeDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse = client.GetAsync("api/Login/GetFoliageTypes").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetFoliageTypeResponse response = JsonConvert.DeserializeObject<GetFoliageTypeResponse>(strData);
                    foliageTypes = response.FoliageTypes;
                }
                else
                {
                    // MessageBox.Show("There was an error retreiving plant types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetFoliageTypes", ex);
                ((App)App.Current).LogError(ex2.Message, String.Empty);
            }
            return foliageTypes;
        }

        public GetFoliageResponse GetFoliageByType(long foliageTypeId)
        {
            GetFoliageResponse foliage = new GetFoliageResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetFoliageByType?foliageTypeId=" + foliageTypeId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    foliage = JsonConvert.DeserializeObject<GetFoliageResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving foliage");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetFoliageByType", ex);
                ((App)App.Current).LogError(ex2.Message, "foliageTypeId = " + foliageTypeId.ToString());
            }

            return foliage;
        }

        private void FoliageSize_SelectedIndexChanged(object sender, EventArgs e)
        {


        }

        private void FoliageName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                FoliageSize.SelectedIndex = -1;

                long selectedValue = ((KeyValuePair<long, string>)FoliageName.SelectedItem).Key;
                string selectedFoliageName = ((KeyValuePair<long, string>)FoliageName.SelectedItem).Value;

                ObservableCollection<FoliageInventoryDTO> fDTO = new ObservableCollection<FoliageInventoryDTO>();

                foreach (FoliageInventoryDTO f in foliage.Where(a => a.Foliage.FoliageName == selectedFoliageName))
                {
                    fDTO.Add(f);
                }

                foliageListView.ItemsSource = fDTO;
            }
            catch(Exception ex)
            {

            }
        }

        private void FoliageType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                long selectedValue = ((KeyValuePair<long, string>)FoliageType.SelectedItem).Key;

                GetFoliageResponse response = GetFoliageByType(selectedValue);

                foliage = response.FoliageInventoryList;

                ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

                foreach (FoliageInventoryDTO resp in foliage)
                {
                    list2.Add(new KeyValuePair<long, string>(resp.Foliage.FoliageId, resp.Foliage.FoliageName));
                }

                FoliageName.ItemsSource = list2;

                ObservableCollection<FoliageInventoryDTO> fDTO = new ObservableCollection<FoliageInventoryDTO>();

                foreach (FoliageInventoryDTO f in foliage.Where(a => a.Foliage.FoliageTypeId == selectedValue))
                {
                    fDTO.Add(f);
                }

                foliageListView.ItemsSource = fDTO;

            }
            catch(Exception ex)
            {

            }
        }

        private void ViewImage_Clicked(object sender, EventArgs e)
        {
            IReadOnlyList<Rg.Plugins.Popup.Pages.PopupPage> popupStack = Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack;

            //One at a time, please
            if (popupStack != null && popupStack.Count > 0)
            {
                return;
            }

            Button b = sender as Button;
            b.IsEnabled = false;

            try
            {
                FoliageInventoryDTO foliage = (FoliageInventoryDTO)((Button)sender).BindingContext;

                if (foliage != null)
                {
                    long foliageImageId = ((App)App.Current).MissingImageId;
                    if (foliage.ImageId != 0)
                    {
                        foliageImageId = foliage.ImageId;
                    }

                    EOImgData img = ((App)App.Current).GetImage(foliageImageId);

                    if (foliageImageId == ((App)App.Current).MissingImageId)
                    {
                        MessagingCenter.Send<FoliageInventoryDTO>(foliage, "FoliageMissingImage");
                    }

                    ((App)App.Current).GetServiceCodeById(foliage.Inventory.ServiceCodeId).ContinueWith(a => ShowImage(img, a.Result));
                }
            }
            catch (Exception ex)
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

            PopupImagePage popup = new PopupImagePage(img, price);

            Navigation.PushPopupAsync(popup);
        }

        private void Help_FoliagePage_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(HelpPage)))
            {
                TaskAwaiter t = Navigation.PushAsync(new HelpPage("FoliagePage")).GetAwaiter();
            }
        }
    }
}