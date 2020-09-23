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
	public partial class ContainersPage : EOBasePage //ContentPage
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

        List<ContainerInventoryDTO> containers = new List<ContainerInventoryDTO>();

        ObservableCollection<ContainerInventoryDTO> list2 = new ObservableCollection<ContainerInventoryDTO>();

        public ContainersPage ()
		{
			InitializeComponent ();

            List<ContainerTypeDTO> containerTypes = GetContainerTypes();

            ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();

            foreach (ContainerTypeDTO code in containerTypes)
            {
                list1.Add(new KeyValuePair<long, string>(code.ContainerTypeId, code.ContainerTypeName));
            }

            ContainerType.ItemsSource = list1;

            ContainerType.SelectedIndexChanged += ContainerType_SelectedIndexChanged;

            ContainerName.SelectedIndexChanged += ContainerName_SelectedIndexChanged;

            ContainerSize.SelectedIndexChanged += ContainerSize_SelectedIndexChanged;

            containers = GetContainers().ContainerInventoryList;

            foreach(ContainerInventoryDTO c in containers)
            {
                list2.Add(c);
            }

            containerListView.ItemsSource = list2;
        }

        public GetContainerResponse GetContainers()
        {
            GetContainerResponse response = new GetContainerResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetContainers").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    response = JsonConvert.DeserializeObject<GetContainerResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plant types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetContainers", ex);
                ((App)App.Current).LogError(ex2.Message, String.Empty);
            }
            return response;
        }

        public List<ContainerTypeDTO> GetContainerTypes()
        {
            List<ContainerTypeDTO> containerTypes = new List<ContainerTypeDTO>();

            try
            {
                HttpClient client = new HttpClient();
                //client.BaseAddress = new Uri("http://localhost:9000/");
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse = client.GetAsync("api/Login/GetContainerTypes").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetContainerTypeResponse response = JsonConvert.DeserializeObject<GetContainerTypeResponse>(strData);
                    containerTypes = response.ContainerTypeList;
                }
                else
                {
                    // MessageBox.Show("There was an error retreiving plant types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetContainerTypes", ex);
                ((App)App.Current).LogError(ex2.Message, String.Empty);
            }
            return containerTypes;
        }

        public GetContainerResponse GetContainerByType(long containerTypeId)
        {
            GetContainerResponse container = new GetContainerResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetContainersByType?containerTypeId=" + containerTypeId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    container = JsonConvert.DeserializeObject<GetContainerResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving containers");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetContainersByType", ex);
                ((App)App.Current).LogError(ex2.Message, "containerTypeId = " + containerTypeId.ToString());
            }

            return container;
        }

        private void ContainerSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            long selectedValue = ((KeyValuePair<long, string>)ContainerSize.SelectedItem).Key;
            string selectedContainerSize = ((KeyValuePair<long, string>)ContainerSize.SelectedItem).Value;

            ObservableCollection<ContainerInventoryDTO> cDTO = new ObservableCollection<ContainerInventoryDTO>();

            foreach (ContainerInventoryDTO c in containers.Where(a => a.Container.ContainerSize.Contains(selectedContainerSize)))
            {
                cDTO.Add(c);
            }

            containerListView.ItemsSource = cDTO;

        }

        private void ContainerName_SelectedIndexChanged(object sender, EventArgs e)
        {
            ContainerSize.SelectedIndex = -1;

            try
            {
                long selectedValue = ((KeyValuePair<long, string>)ContainerName.SelectedItem).Key;
                string selectedContainerName = ((KeyValuePair<long, string>)ContainerName.SelectedItem).Value;

                ObservableCollection<ContainerInventoryDTO> cDTO = new ObservableCollection<ContainerInventoryDTO>();

                foreach (ContainerInventoryDTO c in containers.Where(a => a.Container.ContainerName == selectedContainerName))
                {
                    cDTO.Add(c);
                }

                containerListView.ItemsSource = cDTO;
            }
            catch(Exception ex)
            {
                //log
            }
        }

        private void ContainerType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                long selectedValue = ((KeyValuePair<long, string>)ContainerType.SelectedItem).Key;

                GetContainerResponse response = GetContainerByType(selectedValue);

                containers = response.ContainerInventoryList;

                ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

                foreach (ContainerInventoryDTO resp in containers)
                {
                    list2.Add(new KeyValuePair<long, string>(resp.Container.ContainerId, resp.Container.ContainerName));
                }

                ContainerName.ItemsSource = list2;

                ObservableCollection<ContainerInventoryDTO> cDTO = new ObservableCollection<ContainerInventoryDTO>();

                foreach (ContainerInventoryDTO c in containers.Where(a => a.Container.ContainerTypeId == selectedValue))
                {
                    cDTO.Add(c);
                }

                containerListView.ItemsSource = cDTO;

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
                ContainerInventoryDTO container = (ContainerInventoryDTO)((Button)sender).BindingContext;

                if (container != null)
                {
                    long containerImageId = ((App)App.Current).MissingImageId;
                    if (container.ImageId != 0)
                    {
                        containerImageId = container.ImageId;
                    }

                    EOImgData img = ((App)App.Current).GetImage(containerImageId);

                    if (containerImageId == ((App)App.Current).MissingImageId)
                    {
                        MessagingCenter.Send<ContainerInventoryDTO>(container, "ContainerMissingImage");
                    }

                    ((App)App.Current).GetServiceCodeById(container.Inventory.ServiceCodeId).ContinueWith(a => ShowImage(img, a.Result));
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

            Device.BeginInvokeOnMainThread(() => 
            {
                PopupImagePage popup = new PopupImagePage(img, price);
            });
        }
    }
}