using Android.Content.Res;
using EO.ViewModels.ControllerModels;
using EOMobile.Interfaces;
using Newtonsoft.Json;
using SharedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace EOMobile
{
    public partial class App : Xamarin.Forms.Application
    {
        //Static variables for the app
        public static string EOImageId
        {
            get { 
                string dt = DateTime.Now.ToShortDateString();
                dt = dt.Replace('/', '-');

                return "EOImage_" + dt + "_";   
            }
            set { }
        }

        public static string DefaultImageId = "defaultImage";

        public static string ImageIdToSave = null;

        //Publishable key = pk_test_qEqBdPz6WTh3CNdcc9bgFXpz00haS1e8hC
        //Secret key = sk_test_6vJyMV6NxHArGV6kI2EL6R7V00kzjXJ72u
        //stripe emrgency security bypass code = dvbb-omgm-gpro-rrlv-lcoj
        public string LAN_Address { get; set; }

        public string User { get; set; }

        public string Pwd { get; set; }

        public long MissingImageId { get { return 264L; } }

        public ArrangementInventoryDTO searchedForArrangementInventory { get; set; }

        public ShipmentInventoryItemDTO searchedForShipmentInventory { get; set; }

        public WorkOrderInventoryItemDTO searchedForInventory { get; set; }

        public PersonAndAddressDTO searchedForPerson { get; set; }

        public PersonAndAddressDTO searchedForDeliveryRecipient { get; set; }

        public static List<EOImgData> imageDataList = new List<EOImgData>();

        List<string> pngFileNames;

        List<string> usStateList;

        public App()
        {
            InitializeComponent();

            pngFileNames = new List<string>();

            //LAN_Address = "http://10.0.0.5:9000/";   //Me Fl

            //LAN_Address = "http://10.1.10.148:9000/";   //Me EO

            //LAN_Address = "http://10.1.10.1:9000/";   //router EO

            LAN_Address = "http://10.1.10.36:9000/";   //Roseanne EO

            //LAN_Address = "http://elegantsystem3.ddns.net:9000";   //The farm NoIP ( I had to add port number)

            //LAN_Address = "http://76.109.59.49:9000";   //The farm by ip

            //LAN_Address = "http://eo.hopto.org:9000/";   //Me

            //LAN_Address = "http://192.168.1.134:9000/";  //Thom

            //Stripe.StripeConfiguration.ApiKey = "sk_test_6vJyMV6NxHArGV6kI2EL6R7V00kzjXJ72u";

            MainPage = new NavigationPage(new LoginPage());

            MessagingCenter.Subscribe<ArrangementInventoryDTO>(this, "SearchArrangementInventory", (arg) =>
            {
                LoadArrangementInventory(arg);
            });

            MessagingCenter.Subscribe<ShipmentInventoryItemDTO>(this, "SearchShipmentInventory", (arg) =>
            {
                LoadShipmentInventory(arg);
            });

            MessagingCenter.Subscribe<WorkOrderInventoryItemDTO>(this, "SearchInventory", (arg) =>
            {
                LoadInventory(arg);
            });

            MessagingCenter.Subscribe<PersonAndAddressDTO>(this, "SearchCustomer", (arg) =>
            {
                LoadCustomer(arg);
            });

            MessagingCenter.Subscribe<PersonAndAddressDTO>(this, "SearchDeliveryRecipient", (arg) =>
            {
                LoadDeliveryRecipient(arg);
            });

            MessagingCenter.Subscribe<string>(this, "ImageSelected", async (arg) =>
            {
                PictureSelected(arg);
            });

            MessagingCenter.Subscribe<EOImgData>(this, "ImageSaved", async (arg) =>
            {
                PictureTaken(arg);
            });

            MessagingCenter.Subscribe<PlantInventoryDTO>(this, "PlantMissingImage", async (arg) =>
            {
                NotifyMissingImage(arg);
            });

            MessagingCenter.Subscribe<ContainerInventoryDTO>(this, "ContainerMissingImage", async (arg) =>
            {
                NotifyMissingImage(arg);
            });

            MessagingCenter.Subscribe<FoliageInventoryDTO>(this, "FoliageMissingImage", async (arg) =>
            {
                NotifyMissingImage(arg);
            });

            MessagingCenter.Subscribe<MaterialInventoryDTO>(this, "MaterialMissingImage", async (arg) =>
            {
                NotifyMissingImage(arg);
            });

            InitStateList();
        }

        public List<UserDTO> GetUsers()
        {
            List<UserDTO> users = new List<UserDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetUsers").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();

                    GetUserResponse userResponse = JsonConvert.DeserializeObject<GetUserResponse>(strData);
                    users = userResponse.Users;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plants");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetUsers", ex);
                LogError(ex2.Message, String.Empty);
            }

            return users;
        }

        public List<string> GetStateNames()
        {
            return usStateList;
        }
        public void LoadArrangementInventory(ArrangementInventoryDTO arg)
        {
            searchedForArrangementInventory = arg;
        }

        public void LoadShipmentInventory(ShipmentInventoryItemDTO arg)
        {
            searchedForShipmentInventory = arg;
        }

        public void LoadInventory(WorkOrderInventoryItemDTO arg)
        {
            searchedForInventory = arg;
        }

        public void LoadCustomer(PersonAndAddressDTO arg)
        {
            searchedForPerson = arg;
        }

        public void LoadDeliveryRecipient(PersonAndAddressDTO arg)
        {
            searchedForDeliveryRecipient = arg;
        }

        public void PictureSelected(string arg)
        {
            pngFileNames.Add(arg);
        }

        public List<EOImgData> GetImageData()
        {
            return imageDataList;
        }

        public void AddImageData(EOImgData imageData)
        {
            imageDataList.Add(imageData);
        }
        public void ClearImageData()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                DependencyService.Get<ICameraInterface>().DeleteImageFromStorage(imageDataList);
            });
        }
        public void PictureTaken(EOImgData arg)
        {
            imageDataList.Add(arg);

            MessagingCenter.Send<EOImgData>(arg, "PictureTaken");
        }

        public void NotifyMissingImage(object arg)
        {
            EmailHelpers emailHelper = new EmailHelpers();

            SendInfoEmail(emailHelper.ComposeMissingImage(arg));
        }

        public void ClearNavigationStacks()
        {

        }
        private void SendInfoEmail(string emailHtml)
        {
            try
            {
                EOMailMessage mailMessage = new EOMailMessage("service@elegantorchids.com", "information@elegantorchids.com", "Missing Image", emailHtml, "Orchids@5185");

                Email.SendEmail(mailMessage);
            }
            catch(Exception ex)
            {
                Exception ex2 = new Exception("SendInfoEmail", ex);
                LogError(ex2.Message, "emailHtml = " + emailHtml);
            }
        }

        public void ClearImageDataList()
        {
            imageDataList.Clear();
        }

        public ServiceCodeDTO GetServiceCodeById(long serviceCodeId)
        {
            ServiceCodeDTO serviceCode = new ServiceCodeDTO();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetServiceCodeById?serviceCodeId=" + serviceCodeId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();

                    serviceCode = JsonConvert.DeserializeObject<ServiceCodeDTO>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plants");
                }
            }
            catch(Exception ex)
            {
                Exception ex2 = new Exception("GetServiceCodeById", ex);
                LogError(ex2.Message, "serviceCodeId = " + serviceCodeId.ToString());
            }

            return serviceCode;
        }

        public List<string> GetSizeByInventoryType(long inventoryTypeId)
        {
            List<string> sizes = new List<string>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetSizeByInventoryType?inventoryTypeId=" + inventoryTypeId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();

                    GetSizeResponse sizeResponse = JsonConvert.DeserializeObject<GetSizeResponse>(strData);
                    sizes = sizeResponse.Sizes;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plants");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetSizeByInventoryType", ex);
                LogError(ex2.Message, "inventoryTypeId = " + inventoryTypeId.ToString());
            }

            return sizes;
        }

        public GetArrangementResponse GetArrangement(long arrangementId)
        {
            GetArrangementResponse response = new GetArrangementResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetArrangement?arrangementId=" + arrangementId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    response = JsonConvert.DeserializeObject<GetArrangementResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving arrangements");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetArrangement", ex);
                LogError(ex2.Message, "arrangementId = " + arrangementId.ToString());
            }

            return response;
        }

        public List<GetSimpleArrangementResponse> GetArrangements(string arrangementName)
        {
            //List<ArrangementInventoryDTO> arrangements = new List<ArrangementInventoryDTO>();

            List<GetSimpleArrangementResponse> arrangements = new List<GetSimpleArrangementResponse>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetArrangements?arrangementName=" + arrangementName).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    List<GetSimpleArrangementResponse> response = JsonConvert.DeserializeObject<List<GetSimpleArrangementResponse>>(strData);

                    //arrangements = response.ArrangementList;
                    arrangements = response;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving arrangements");
                }
            }
            catch(Exception ex)
            {
                Exception ex2 = new Exception("GetArrangements", ex);
                LogError(ex2.Message, "arrangementName = " + arrangementName);
            }

            return arrangements;
        }

        public EOImgData GetImage(long imageId)
        {
            EOImgData imageData = new EOImgData();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetImage?imageId=" + Convert.ToString(imageId)).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    imageData.ImageId = imageId;
                    imageData.imgData =  httpResponse.Content.ReadAsByteArrayAsync().Result;
                    imageData.isNewImage = false;
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetImage", ex);
                LogError(ex2.Message, "imageId = " + imageId.ToString());
            }

            return imageData;
        }

        public List<InventoryTypeDTO> GetInventoryTypes()
        {
            List<InventoryTypeDTO> dtoList = new List<InventoryTypeDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);

                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));


                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                   client.GetAsync("api/Login/GetInventoryTypes").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetInventoryTypeResponse response = JsonConvert.DeserializeObject<GetInventoryTypeResponse>(strData);
                    dtoList = response.InventoryType;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving inventory types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetInventoryTypes", ex);
                LogError(ex2.Message, String.Empty);
            }
            return dtoList;
        }

        public List<PersonAndAddressDTO> GetCustomers(GetPersonRequest request)
        {
            List<PersonAndAddressDTO> people = new List<PersonAndAddressDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse =
                    client.PostAsync("api/Login/GetPerson",content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetPersonResponse resp = JsonConvert.DeserializeObject<GetPersonResponse>(strData);
                    people= resp.PersonAndAddress;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving people");
                }
            }
            catch(Exception ex)
            {
                Exception ex2 = new Exception("GetCustomers", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }

            return people;
        }

        public List<VendorDTO> GetVendors(GetPersonRequest request)
        {
            List<VendorDTO> vDTO = new List<VendorDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse =
                    client.PostAsync("api/Login/GetVendors",content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetVendorResponse resp = JsonConvert.DeserializeObject<GetVendorResponse>(strData);
                    vDTO = resp.VendorList;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving vendors");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetVendors", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }

            return vDTO;
        }

        public long AddShipment(AddShipmentRequest request)
        {
            long newShipmentId = 0;
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/AddShipment", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    ApiResponse apiResponse = JsonConvert.DeserializeObject<ApiResponse>(strData);

                    newShipmentId = apiResponse.Id;

                    if (apiResponse.Messages.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (KeyValuePair<string, List<string>> messages in apiResponse.Messages)
                        {
                            foreach (string msg in messages.Value)
                            {
                                sb.AppendLine(msg);
                            }
                        }

                        //MessageBox.Show(sb.ToString());
                    }
                    else
                    {
                        //this.WorkOrderInventoryListView.ItemsSource = null;
                    }
                }
                else
                {
                    //MessageBox.Show("Error adding Work Order");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("AddShipment", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }

            return newShipmentId;
        }

        public ShipmentInventoryDTO GetShipment(long shipmentId)
        {
            ShipmentInventoryDTO response = new ShipmentInventoryDTO();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                //string jsonData = JsonConvert.SerializeObject(filter);
                //var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetShipment?shipmentId=" + Convert.ToString(shipmentId)).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    response = JsonConvert.DeserializeObject<ShipmentInventoryDTO>(strData);
                }
            }
            catch(Exception ex)
            {
                Exception ex2 = new Exception("GetShipment", ex);
                LogError(ex2.Message, "shipmentId = " + shipmentId.ToString());
            }

            return response;
        }

        public List<ShipmentInventoryDTO> GetShipments(ShipmentFilter filter)
        {
            List<ShipmentInventoryDTO> shipments = new List<ShipmentInventoryDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(filter);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse =
                    client.PostAsync("api/Login/GetShipments", content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    GetShipmentResponse response = JsonConvert.DeserializeObject<GetShipmentResponse>(strData);
                    shipments = response.ShipmentList;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving Work Orders");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetShipments", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(filter));
            }

            return shipments;
        }

        public WorkOrderResponse GetWorkOrder(long workOrderId)
        {
            WorkOrderResponse response = new WorkOrderResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                //string jsonData = JsonConvert.SerializeObject(filter);
                //var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetWorkOrder?workOrderId=" + Convert.ToString(workOrderId)).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    response = JsonConvert.DeserializeObject<WorkOrderResponse>(strData);
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetWorkOrder", ex);
                LogError(ex2.Message, "workOrderId = " + workOrderId.ToString());
            }

            return response;
        }

        public List<WorkOrderResponse> GetWorkOrders(WorkOrderListFilter filter)
        {
            List<WorkOrderResponse> workOrders = new List<WorkOrderResponse>();

            try
            {
                //WorkOrderListFilter filter = new WorkOrderListFilter();
                //filter.FromDate = this.FromDatePicker.SelectedDate.Value;
                //filter.ToDate = this.ToDatePicker.SelectedDate.Value;

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(filter);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse =
                    client.PostAsync("api/Login/GetWorkOrders", content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    workOrders = JsonConvert.DeserializeObject<List<WorkOrderResponse>>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving Work Orders");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetWorkOrders", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(filter));
            }

            return workOrders;
        }

        public long AddWorkOrder(AddWorkOrderRequest request)
        {
            long newWorkOrderId = 0;

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/AddWorkOrder", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(strData);
                    newWorkOrderId = response.Id;

                    //ApiResponse apiResponse = JsonConvert.DeserializeObject<ApiResponse>(strData);

                    //if (apiResponse.Messages.Count > 0)
                    //{
                    //    StringBuilder sb = new StringBuilder();
                    //    foreach (KeyValuePair<string, List<string>> messages in apiResponse.Messages)
                    //    {
                    //        foreach (string msg in messages.Value)
                    //        {
                    //            sb.AppendLine(msg);
                    //        }
                    //    }

                    //    //MessageBox.Show(sb.ToString());
                    //}
                    //else
                    //{
                    //    //this.WorkOrderInventoryListView.ItemsSource = null;
                    //}
                }
                else
                {
                    //MessageBox.Show("Error adding Work Order");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("AddWorkOrder", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }

            return newWorkOrderId;
        }

        public long AddWorkOrderPayment(WorkOrderPaymentDTO  request)
        {
            long newWorkOrderPaymentId = 0;

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/AddWorkOrderPayment", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(strData);
                    newWorkOrderPaymentId = response.Id;
                }
                else
                {
                    //MessageBox.Show("Error adding Work Order");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("AddWorkOrderPayment", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }

            return newWorkOrderPaymentId;
        }

        public WorkOrderPaymentDTO GetWorkOrderPayment(long workOrderId)
        {
            WorkOrderPaymentDTO workOrderPayment = new WorkOrderPaymentDTO();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetWorkOrderPayment?workOrderId=" + workOrderId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    workOrderPayment = JsonConvert.DeserializeObject<WorkOrderPaymentDTO>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving materials");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetWorkOrderPayment", ex);
                LogError(ex2.Message, "workOrderId = " + workOrderId.ToString());
            }

            return workOrderPayment;
        }

        public List<long> GetWorkOrderImageIds(long workOrderId)
        {
            List<long> workOrderImageIds = new List<long>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetWorkOrderImageIds?workOrderId=" + workOrderId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    WorkOrderImageIdResponse response = JsonConvert.DeserializeObject<WorkOrderImageIdResponse>(strData);

                    if(response != null)
                    {
                        workOrderImageIds = response.ImageIdList;
                    }
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving image ids for work order");
                }
            }
            catch(Exception ex)
            {
                Exception ex2 = new Exception("GetWorkOrderImageIds", ex);
                LogError(ex2.Message, "workOrderId = " + workOrderId.ToString());
            }

            return workOrderImageIds;
        }
        public List<EOImageSource> GetWordOrderImages(long workOrderId)
        {
            List<EOImageSource> workOrderImages = new List<EOImageSource>();

            try
            {
                List<long> imageIds = GetWorkOrderImageIds(workOrderId);

                foreach(long imageId in imageIds)
                {
                    EOImgData img = GetImage(imageId);
                    workOrderImages.Add(new EOImageSource()
                    {
                        ImageId = img.ImageId,
                        Image = img.imgData
                    });
                }
            }
            catch(Exception ex)
            {
                Exception ex2 = new Exception("GetWorkOrderImages", ex);
                LogError(ex2.Message, "workOrderId = " + workOrderId.ToString());
            }

            return workOrderImages;
        }

        public void AddWorkOrderImage(AddWorkOrderImageRequest request)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/AddWorkOrderImage", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    int debug = 1;
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("AddWorkOrderImage", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }
        }

        public bool MarkWorkOrderPaid(MarkWorkOrderPaidRequest request)
        {
            bool success = false;

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/MarkWorkOrderPaid", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("MarkWorkOrderPaid", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }

            return success;
        }

        public long AddArrangement(AddArrangementRequest request)
        {
            long newArrangementId = 0;

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/AddArrangement", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(strData);
                    newArrangementId = response.Id;
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("AddArrangement", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }

            return newArrangementId;
        }

        public void AddArrangementImage(AddArrangementImageRequest request)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/AddArrangementImage", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    int debug = 1;
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("AddArrangementImage", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }
        }

        public long UpdateArrangement(UpdateArrangementRequest request)
        {
            long arrangementId = 0;

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/UpdateArrangement", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(strData);
                    arrangementId = response.Id;
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("UpdateArrangement", ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }

            return arrangementId;
        }

        public bool DeleteArrangement(long arrangementId)
        {
            bool arrangementDeleted = false;

            return arrangementDeleted;
        }

        public List<MaterialTypeDTO> GetMaterialTypes()
        {
            List<MaterialTypeDTO> materialTypes = new List<MaterialTypeDTO>();

            try
            {
                HttpClient client = new HttpClient();
                //client.BaseAddress = new Uri("http://localhost:9000/");
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse = client.GetAsync("api/Login/GetMaterialTypes").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetMaterialTypeResponse response = JsonConvert.DeserializeObject<GetMaterialTypeResponse>(strData);
                    materialTypes = response.MaterialTypes;
                }
                else
                {
                    // MessageBox.Show("There was an error retreiving plant types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetMaterialTypes", ex);
                LogError(ex2.Message, String.Empty);
            }
            return materialTypes;
        }

        public List<FoliageTypeDTO> GetFoliageTypes()
        {
            List<FoliageTypeDTO> foliageTypes = new List<FoliageTypeDTO>();

            try
            {
                HttpClient client = new HttpClient();
                //client.BaseAddress = new Uri("http://localhost:9000/");
                client.BaseAddress = new Uri(LAN_Address);
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
                LogError(ex2.Message, String.Empty);
            }
            return foliageTypes;
        }
        public GetMaterialResponse GetMaterialByType(long materialTypeId)
        {
            GetMaterialResponse materials = new GetMaterialResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
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
                    materials = JsonConvert.DeserializeObject<GetMaterialResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving materials");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetMaterialByType", ex);
                LogError(ex2.Message, "materialTypeId = " + materialTypeId.ToString());
            }

            return materials;
        }
        public List<MaterialNameDTO> GetMaterialNamesByType(long materialTypeId)
        {
            List<MaterialNameDTO> materialNameList = new List<MaterialNameDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetMaterialNamesByType?materialTypeId=" + materialTypeId.ToString()).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetMaterialNameResponse response = JsonConvert.DeserializeObject<GetMaterialNameResponse>(strData);

                    materialNameList = response.MaterialNames;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plant names");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetMaterialNamesByType", ex);
                LogError(ex2.Message, "materialTypeId = " + materialTypeId.ToString());
            }

            return materialNameList;
        }

        public GetFoliageResponse GetFoliageByType(long foliageTypeId)
        {
            GetFoliageResponse foliage = new GetFoliageResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
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
                    //MessageBox.Show("There was an error retreiving materials");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetFoliageByType", ex);
                LogError(ex2.Message, "foliageTypeId = " + foliageTypeId.ToString());
            }

            return foliage;
        }

        public List<FoliageNameDTO> GetFoliageNamesByType(long foliageTypeId)
        {
            List<FoliageNameDTO> foliageNameList = new List<FoliageNameDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetFoliageNamesByType?foliageTypeId=" + foliageTypeId.ToString()).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetFoliageNameResponse response = JsonConvert.DeserializeObject<GetFoliageNameResponse>(strData);

                    foliageNameList = response.FoliageNames;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plant names");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetFoliageNamesByType", ex);
                LogError(ex2.Message, "foliageTypeId = " + foliageTypeId.ToString());
            }

            return foliageNameList;
        }

        public GetPlantResponse GetPlants()
        {
            GetPlantResponse response = new GetPlantResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetPlants").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    response = JsonConvert.DeserializeObject<GetPlantResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plant types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetPlants", ex);
                LogError(ex2.Message, String.Empty);
            }
            return response;
        }

        public List<PlantTypeDTO> GetPlantTypes()
        {
            List<PlantTypeDTO> plantTypes = new List<PlantTypeDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetPlantTypes").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetPlantTypeResponse response = JsonConvert.DeserializeObject<GetPlantTypeResponse>(strData);
                    plantTypes = response.PlantTypes;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plant types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetPlantTypes", ex);
                LogError(ex2.Message, String.Empty);
            }

            return plantTypes;
        }

        public List<PlantNameDTO> GetPlantNamesByType(long plantTypeId)
        {
            List<PlantNameDTO> plantNameList = new List<PlantNameDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetPlantNamesByType?plantTypeId=" + plantTypeId.ToString()).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetPlantNameResponse response = JsonConvert.DeserializeObject<GetPlantNameResponse>(strData);

                    plantNameList = response.PlantNames;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plant names");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetPlantNamesByType", ex);
                LogError(ex2.Message, "plantTypeId = " + plantTypeId.ToString());
            }

            return plantNameList;
        }

        public GetPlantResponse GetPlantsByType(long plantTypeId)
        {
            GetPlantResponse plants = new GetPlantResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
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
                LogError(ex2.Message, "plantTypeId = " + plantTypeId.ToString());
            }

            return plants;
        }

        public List<ContainerNameDTO> GetContainerNamesByType(long containerTypeId)
        {
            List<ContainerNameDTO> containerNameList = new List<ContainerNameDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetContainerNamesByType?containerTypeId=" + containerTypeId.ToString()).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    containerNameList = JsonConvert.DeserializeObject<List<ContainerNameDTO>>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving container names");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetContainerNamesByType", ex);
                LogError(ex2.Message, "containerTypeId = " + containerTypeId.ToString());
            }

            return containerNameList;
        }

        public List<ContainerTypeDTO> GetContainerTypes()
        {
            List<ContainerTypeDTO> containerTypes = new List<ContainerTypeDTO>();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("plain/text"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetContainerTypes").Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    GetContainerTypeResponse response = JsonConvert.DeserializeObject<GetContainerTypeResponse>(strData);
                    containerTypes = response.ContainerTypeList;
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving container types");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("GetContainerTypes", ex);
                LogError(ex2.Message, String.Empty);
            }
            return containerTypes;
        }

        public GetContainerResponse GetContainersByType(long typeId)
        {
            GetContainerResponse containers = new GetContainerResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                HttpResponseMessage httpResponse =
                    client.GetAsync("api/Login/GetContainersByType?containerTypeId=" + typeId).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    containers = JsonConvert.DeserializeObject<GetContainerResponse>(strData);
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plants");
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message, "typeId = " + typeId.ToString());
            }

            return containers;
        }

        public void LogError(string message, string payload)
        {
            try
            {
                ErrorLogRequest request = new ErrorLogRequest();
                request.ErrorLog.Message = message;
                request.ErrorLog.Payload = payload;

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/LogError", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {

                }
            }
            catch(Exception ex)
            {
                //ironic
            }
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        /// <summary>
        /// Retrieve picker index for pickers whose itemsSource is List<KeyValuePair<long, string>> and that contain the passed value
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetPickerIndex(Picker p, long value)
        {
            List<KeyValuePair<long, string>> items = p.ItemsSource as List<KeyValuePair<long, string>>;

            int workIndex = 0;
            int pickerIndex = workIndex;

            if (items != null)
            {
                foreach (KeyValuePair<long, string> kvp in items)
                {
                    if (kvp.Key == value)
                    {
                        break;
                    }
                    else
                    {
                        workIndex++;
                    }
                }

                if (workIndex < items.Count)
                {
                    pickerIndex = workIndex;
                }
            }

            return pickerIndex;
        }

        /*
         *  Setting the file name is really only needed for Android, when in the OnActivityResult method you need
         *  a way to know the file name passed into the intent when launching the camera/gallery. In this case,
         *  1 image will be saved to the file system using the value of App.DefaultImageId, this is required for the 
         *  FileProvider implemenation that is needed on newer Android OS versions. Using the same file name will 
         *  keep overwriting the existing image so you will not fill up the app's memory size over time. 
         * 
         *  This of course assumes your app has NO need to save images locally. But if your app DOES need to save images 
         *  locally, then pass the file name you want to use into the method SetImageFileName (do NOT include the file extension in the name,
         *  that will be handled down the road based on the FileFormatEnum you pick). 
         * 
         *  NOTE: When saving images, if you decide to pick PNG format, you may notice your app runs slower 
         *  when processing the image. If your image doesn't need to respect any Alpha values, use JPEG, it's faster. 
         */
        public string SetImageFileName(string fileName = null)
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                if (fileName != null)
                    App.ImageIdToSave = fileName;
                else
                    App.ImageIdToSave = App.EOImageId;

                return App.ImageIdToSave;
            }
            else
            {
                //To iterate, on iOS, if you want to save images to the devie, set 
                if (fileName != null)
                {
                    App.ImageIdToSave = fileName;
                    return fileName;
                }
                else
                    return null;
            }
        }

        void InitStateList()
        {
            usStateList = new List<string>();

            usStateList.Add("AK");
            usStateList.Add("AL");
            usStateList.Add("AR");
            usStateList.Add("AZ");
            usStateList.Add("CA");
            usStateList.Add("CO");
            usStateList.Add("CT");
            usStateList.Add("DE");
            usStateList.Add("FL");
            usStateList.Add("GA");
            usStateList.Add("HI");
            usStateList.Add("IA");
            usStateList.Add("ID");
            usStateList.Add("IL");
            usStateList.Add("IN");
            usStateList.Add("KS");
            usStateList.Add("KY");
            usStateList.Add("LA");
            usStateList.Add("MA");
            usStateList.Add("MD");
            usStateList.Add("ME");
            usStateList.Add("MI");
            usStateList.Add("MN");
            usStateList.Add("MO");
            usStateList.Add("MS");
            usStateList.Add("MT");
            usStateList.Add("NC");
            usStateList.Add("ND");
            usStateList.Add("NE");
            usStateList.Add("NH");
            usStateList.Add("NJ");
            usStateList.Add("NM");
            usStateList.Add("NV");
            usStateList.Add("NY");
            usStateList.Add("OH");
            usStateList.Add("OK");
            usStateList.Add("OR");
            usStateList.Add("PA");
            usStateList.Add("RI");
            usStateList.Add("SC");
            usStateList.Add("SD");
            usStateList.Add("TN");
            usStateList.Add("TX");
            usStateList.Add("UT");
            usStateList.Add("VA");
            usStateList.Add("VT");
            usStateList.Add("WA");
            usStateList.Add("WI");
            usStateList.Add("WV");
            usStateList.Add("WY");
        }
    }
}
