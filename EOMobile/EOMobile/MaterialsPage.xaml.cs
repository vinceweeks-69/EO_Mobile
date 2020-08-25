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
	public partial class MaterialsPage : EOBasePage
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

        List<MaterialInventoryDTO> materials = new List<MaterialInventoryDTO>();

        ObservableCollection<MaterialInventoryDTO> list2 = new ObservableCollection<MaterialInventoryDTO>();

        public MaterialsPage ()
		{
			InitializeComponent ();

            List<MaterialTypeDTO> materialTypes = ((App)App.Current).GetMaterialTypes();

            ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();

            foreach (MaterialTypeDTO code in materialTypes)
            {
                list1.Add(new KeyValuePair<long, string>(code.MaterialTypeId, code.MaterialTypeName));
            }

            MaterialType.ItemsSource = list1;

            MaterialType.SelectedIndexChanged += MaterialType_SelectedIndexChanged;

            MaterialName.SelectedIndexChanged += MaterialName_SelectedIndexChanged;

            MaterialSize.SelectedIndexChanged += MaterialSize_SelectedIndexChanged;

            materials = GetMaterials().MaterialInventoryList;

            foreach(MaterialInventoryDTO m in materials)
            {
                list2.Add(m);
            }

            materialListView.ItemsSource = list2;
        }

        public GetMaterialResponse GetMaterials()
        {
            GetMaterialResponse response = new GetMaterialResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetMaterials").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    response = JsonConvert.DeserializeObject<GetMaterialResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plant types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetMaterials", ex);
                ((App)App.Current).LogError(ex2.Message, String.Empty);
            }
            return response;
        }
               
        public GetMaterialResponse GetMaterialByType(long materialTypeId)
        {
            GetMaterialResponse material = new GetMaterialResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetMaterialsByType?materialTypeId=" + materialTypeId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    material = JsonConvert.DeserializeObject<GetMaterialResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plants");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetMaterialsByType", ex);
                ((App)App.Current).LogError(ex2.Message, "materialTypeId = " + materialTypeId.ToString());
            }

            return material;
        }

        private void MaterialSize_SelectedIndexChanged(object sender, EventArgs e)
        {


        }

        private void MaterialName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                MaterialSize.SelectedIndex = -1;

                long selectedValue = ((KeyValuePair<long, string>)MaterialName.SelectedItem).Key;
                string selectedMaterialName = ((KeyValuePair<long, string>)MaterialName.SelectedItem).Value;

                //List<GetPlantResponse> plants = GetPlantSizes(selectedValue);

                //ObservableCollection<KeyValuePair<long, string>> list3 = new ObservableCollection<KeyValuePair<long, string>>();

                //foreach (GetPlantResponse resp in plants)
                //{
                //    list2.Add(new KeyValuePair<long, string>(resp.Plant.PlantId, resp.Plant.PlantName));
                //}

                //PlantSize.ItemsSource = list3; 

                ObservableCollection<MaterialInventoryDTO> mDTO = new ObservableCollection<MaterialInventoryDTO>();

                foreach (MaterialInventoryDTO m in materials.Where(a => a.Material.MaterialName == selectedMaterialName))
                {
                    mDTO.Add(m);
                }

                materialListView.ItemsSource = mDTO;
            }
            catch(Exception ex)
            {

            }
        }

        private void MaterialType_SelectedIndexChanged(object sender, EventArgs e)
        {
            long selectedValue = ((KeyValuePair<long, string>)MaterialType.SelectedItem).Key;

            GetMaterialResponse response = GetMaterialByType(selectedValue);

            materials = response.MaterialInventoryList;

            ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

            foreach (MaterialInventoryDTO resp in materials)
            {
                list2.Add(new KeyValuePair<long, string>(resp.Material.MaterialId, resp.Material.MaterialName));
            }

            MaterialName.ItemsSource = list2;

            ObservableCollection<MaterialInventoryDTO> mDTO = new ObservableCollection<MaterialInventoryDTO>();

            foreach (MaterialInventoryDTO m in materials.Where(a => a.Material.MaterialTypeId == selectedValue))
            {
                mDTO.Add(m);
            }

            materialListView.ItemsSource = mDTO;
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
                MaterialInventoryDTO material = (MaterialInventoryDTO)((Button)sender).BindingContext;

                if (material != null)
                {
                    long materialImageId = ((App)App.Current).MissingImageId;
                    if (material.ImageId != 0)
                    {
                        materialImageId = material.ImageId;
                    }

                    EOImgData img = ((App)App.Current).GetImage(materialImageId);

                    ServiceCodeDTO serviceCode = ((App)App.Current).GetServiceCodeById(material.Inventory.ServiceCodeId);

                    string price = string.Empty;
                    if (serviceCode.ServiceCodeId > 0)
                    {
                        price = (serviceCode.Price.HasValue ? serviceCode.Price.Value.ToString("C2", CultureInfo.CurrentCulture) : String.Empty);
                    }

                    PopupImagePage popup = new PopupImagePage(img, price);

                    Navigation.PushPopupAsync(popup);

                    if (materialImageId == ((App)App.Current).MissingImageId)
                    {
                        MessagingCenter.Send<MaterialInventoryDTO>(material, "MaterialMissingImage");
                    }
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

        private void Help_MaterialsPage_Clicked(object sender, EventArgs e)
        {
            TaskAwaiter t = Navigation.PushAsync(new HelpPage("MaterialsPage")).GetAwaiter();
        }
    }
}