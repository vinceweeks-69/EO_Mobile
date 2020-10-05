using Android.Content;
using Android.Content.Res;
using Android.Net.Wifi;
using EO.ViewModels.ControllerModels;
using EOMobile.Interfaces;
using Newtonsoft.Json;
using SharedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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

        public long Role { get; set; }

        public long MissingImageId { get { return 264L; } }

        public ArrangementInventoryDTO searchedForArrangementInventory { get; set; }

        public ShipmentInventoryItemDTO searchedForShipmentInventory { get; set; }

        public WorkOrderInventoryItemDTO searchedForInventory { get; set; }

        public PersonAndAddressDTO searchedForPerson { get; set; }

        public PersonAndAddressDTO searchedForDeliveryRecipient { get; set; }

        public AddArrangementRequest searchedForArrangement { get; set; }

        public CustomerContainerDTO searchedForCustomerContainer { get; set; }

        public static List<EOImgData> imageDataList = new List<EOImgData>();

        List<string> pngFileNames;

        List<string> usStateList;

        public App()
        {
            InitializeComponent();

            pngFileNames = new List<string>();

            //LAN_Address = "http://99.125.200.187:9000"; //Me Royalwood IP

            //LAN_Address = "http://10.0.0.4:9000/";   //Me Royalwood router

            //LAN_Address = "http://10.1.10.148:9000/";   //Me EO

            //LAN_Address = "http://10.1.10.1:9000/";   //router EO

            LAN_Address = "http://10.1.10.36:9000/";   //Roseanne EO when hardwired to eohome network

            //LAN_Address = "http://192.168.1.134:9000"; //EO my laptop on jdambar

            //LAN_Address = "http://elegantsystem2.ddns.net:9000";   //The farm NoIP ( I had to add port number)

            //LAN_Address = "http://elegantsystem.ddns.net:9000";   //The farm by ip

            //LAN_Address = "http://eo.hopto.org:9000/";   //Me

            //LAN_Address = "http://192.168.1.134:9000/";  //Thom

            //Stripe.StripeConfiguration.ApiKey = "sk_test_6vJyMV6NxHArGV6kI2EL6R7V00kzjXJ72u";

            string hostName = DependencyService.Get<IWifiActivity>().GetHostName();

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

            MessagingCenter.Subscribe<AddArrangementRequest>(this, "AddArrangementToWorkOrder", (arg) =>
            {
                AddArrangementToWorkOrder(arg);
            });

            MessagingCenter.Subscribe<CustomerContainerDTO>(this, "AddCustomerContainerToWorkOrder", (arg) =>
            {
                AddCustomerContainerToWorkOrder(arg);
            });

            InitStateList();
        }

        public void ChangeLANAddress(string hostName)
        {
            hostName = hostName.ToLower();

            if(hostName.Contains("jdambar") ||
                hostName.Contains("orchidhome"))
            {
                LAN_Address = "http://10.1.10.36:9000";             //Roseanne
                //LAN_Address = "http://192.168.1.134:9000";    //Me in back office
            }
            else
            {
                LAN_Address = "http://elegantsystem2.ddns.net:9000";  //76.109.59.49
            }
        }
        
        public List<string> GetLocalIPv4(NetworkInterfaceType _type)
        {
            List<string> ipAddrList = new List<string>();
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipAddrList.Add(ip.Address.ToString());
                        }
                    }
                }
            }
            return ipAddrList;
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

        public void AddArrangementToWorkOrder(AddArrangementRequest arg)
        {
            searchedForArrangement = arg;
        }

        public void AddCustomerContainerToWorkOrder(CustomerContainerDTO arg)
        {
            searchedForCustomerContainer = arg;
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

        //where the call you want make is paramName = objectPkId, objectName (string)  OR leave paramName and paramID empty for "Get All"
        public async Task<T> GetRequest<T>(GenericGetRequest getRequest) where T : new()
        {
            try
            {
                string webServiceAdx = "api/login/" + getRequest.Uri;

                if(!String.IsNullOrEmpty(getRequest.ParamName))
                {
                    webServiceAdx += "?" + getRequest.ParamName + "=";

                    if (!String.IsNullOrEmpty(getRequest.ParamValue))
                    {
                        webServiceAdx += getRequest.ParamValue;
                    }
                    else
                    {
                        webServiceAdx += getRequest.ParamId.ToString();
                    }
                }

                var client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                using (HttpResponseMessage httpResponse = await
                    client.GetAsync(webServiceAdx))
                {
                    httpResponse.EnsureSuccessStatusCode();
                    
                    Stream streamData = await httpResponse.Content.ReadAsStreamAsync();
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(strData);
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception(getRequest.Uri, ex);
                LogError(ex2.Message, JsonConvert.SerializeObject(getRequest));
                //this returns null for reference types
                return new T();
            }
        }

        public async Task<TOut> PostRequest<TIn, TOut>(string uri, TIn content) where TOut : new()
        {
            string serializedContent = String.Empty;

            try
            {
                string webServiceAdx = "api/login/" + uri;

                var client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                serializedContent = JsonConvert.SerializeObject(content);
                StringContent serialized = new StringContent(serializedContent, Encoding.UTF8, "application/json");

                using(HttpResponseMessage response = await client.PostAsync(webServiceAdx, serialized))
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<TOut>(responseBody);
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception(uri, ex);
                LogError(ex2.Message, serializedContent);
                return new TOut();
            }
        }


        public async Task<GetUserResponse> GetUsers()
        {
            GenericGetRequest request = new GenericGetRequest("GetUsers",String.Empty, 0); 
            GetUserResponse response = await GetRequest<GetUserResponse>(request);
            return response;
        }   

        public async Task<ApiResponse> DoesCustomerExist(AddCustomerRequest request)
        {
            ApiResponse taskResponse = await PostRequest<AddCustomerRequest, ApiResponse>("DoesCustomerExist", request);
            return taskResponse;
        }

        //Called Get - actually a Post
        public async Task<GetPersonResponse> GetCustomer(long customerId)
        {
            GetPersonRequest request = new GetPersonRequest();
            request.PersonId = customerId;

            GetPersonResponse taskResponse = await PostRequest<GetPersonRequest, GetPersonResponse>("GetPerson", request);

            return taskResponse;
        }

        public async Task<ApiResponse>AddImage(AddImageRequest request)
        {
            ApiResponse taskResponse = await PostRequest<AddImageRequest, ApiResponse>("AddImage", request);
            return taskResponse;
        }

        public async Task<CustomerContainerResponse> GetCustomerContainers(long customerId)
        {
            CustomerContainerRequest request = new CustomerContainerRequest();
            request.CustomerContainer.CustomerId = customerId;
            CustomerContainerResponse response = await PostRequest<CustomerContainerRequest, CustomerContainerResponse>("GetCustomerContainers", request);
            return response;
        }

        public async Task<ApiResponse> AddUpdateCustomerContainers(CustomerContainerRequest request)
        {
            ApiResponse response = await PostRequest<CustomerContainerRequest, CustomerContainerResponse>("AddUpdateCustomerContainer", request);
            return response;
        }

        public async Task<ApiResponse> DeleteCustomerContainer(CustomerContainerRequest request)
        {
            ApiResponse response = await PostRequest<CustomerContainerRequest, CustomerContainerResponse>("DeleteCustomerContainer", request);
            return response;
        }

        public async Task<ServiceCodeDTO> GetServiceCodeById(long serviceCodeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetServiceCodeById", "serviceCodeId", serviceCodeId);
            ServiceCodeDTO response = await GetRequest<ServiceCodeDTO>(request);
            return response;
        }

        public async Task<GetSizeResponse> GetSizeByInventoryType(GenericGetRequest request)
        {
            GetSizeResponse response = await GetRequest<GetSizeResponse>(request);
            return response;
        }

        public async Task<GetPlantTypeResponse> GetPlantTypes(GenericGetRequest request)
        {
            GetPlantTypeResponse response = await GetRequest<GetPlantTypeResponse>(request);
            return response;
        }

        public bool ArrangementNameIsNotUnique(ArrangementDTO arrangement)
        {
            ApiResponse response = new ApiResponse();

            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                string jsonData = JsonConvert.SerializeObject(arrangement);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/ArrangementNameIsNotUnique", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    string strData = httpResponse.Content.ReadAsStringAsync().Result;
                    response = JsonConvert.DeserializeObject<ApiResponse>(strData);
                }
                else
                {
                    List<string> errorMsgs = new List<string>();
                    errorMsgs.Add(httpResponse.ReasonPhrase);
                    response.Messages.Add("ArrangementNameIsUnique", errorMsgs);
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("ArrangementNameIsNotUnique", ex);
                LogError(ex2.Message, "arrangementId = " + arrangement.ArrangementId.ToString());
            }

            return response.Success;
        }

        public async Task<GetArrangementResponse> GetArrangement(long arrangementId)
        {
            GenericGetRequest request = new GenericGetRequest("GetArrangement", "arrangementId", arrangementId);
            GetArrangementResponse response = await GetRequest<GetArrangementResponse>(request);
            return response;
        }

        public async Task<List<GetSimpleArrangementResponse>> GetArrangements(string arrangementName)
        {
            GenericGetRequest request = new GenericGetRequest("GetArrangements", "arrangementName", arrangementName);
            List<GetSimpleArrangementResponse> response = await GetRequest<List<GetSimpleArrangementResponse>>(request);
            return response;
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

        public async Task<GetInventoryTypeResponse> GetInventoryTypes()
        {
            GenericGetRequest request = new GenericGetRequest("GetInventoryTypes", String.Empty, 0);
            GetInventoryTypeResponse response = await GetRequest<GetInventoryTypeResponse>(request);
            return response;
        }

        public async Task<GetPersonResponse> GetCustomers(GetPersonRequest request)
        {
            GetPersonResponse response = await PostRequest<GetPersonRequest, GetPersonResponse>("GetPerson", request);
            return response;
        }

        public async Task<GetVendorResponse> GetVendors(GetPersonRequest request)
        {
            GetVendorResponse response = await PostRequest<GetPersonRequest, GetVendorResponse>("GetVendors", request);
            return response;
        }

        public long AddShipment(AddShipmentRequest request)
        {
            long newShipmentId = 0;
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(LAN_Address);
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

        public async Task<ShipmentInventoryDTO> GetShipment(long shipmentId)
        {
            GenericGetRequest request = new GenericGetRequest("GetShipment", "shipmentId",shipmentId);
            ShipmentInventoryDTO response = await GetRequest<ShipmentInventoryDTO>(request);
            return response;
        }
        public async Task<GetShipmentResponse> GetShipments(ShipmentFilter filter)
        {
            GetShipmentResponse response = await PostRequest<ShipmentFilter, GetShipmentResponse>("GetShipments", filter);
            return response;
        }


        public async Task<WorkOrderResponse> GetWorkOrder(long workOrderId)
        {
            GenericGetRequest request = new GenericGetRequest("GetWorkOrder", "workOrderId", workOrderId);
            WorkOrderResponse response = await GetRequest<WorkOrderResponse>(request);
            return response;
        }

        public async Task<List<WorkOrderResponse>> GetWorkOrders(WorkOrderListFilter filter)
        {
            List<WorkOrderResponse> response = await PostRequest<WorkOrderListFilter, List<WorkOrderResponse>>("GetWorkOrders", filter);
            return response;
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

        public async Task<WorkOrderPaymentDTO> GetWorkOrderPayment(long workOrderId)
        {
            GenericGetRequest request = new GenericGetRequest("GetWorkOrderPayment", "workOrderId", workOrderId);
            WorkOrderPaymentDTO response = await GetRequest<WorkOrderPaymentDTO>(request);
            return response;
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

        public long UpdateArrangement(AddArrangementRequest request)
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

        public async Task<GetMaterialTypeResponse> GetMaterialTypes()
        {
            GenericGetRequest request = new GenericGetRequest("GetMaterialTypes", String.Empty, 0);
            GetMaterialTypeResponse response = await GetRequest<GetMaterialTypeResponse>(request);
            return response;
        }

        public async Task<GetFoliageTypeResponse> GetFoliageTypes()
        {
            GenericGetRequest request = new GenericGetRequest("GetFoliageTypes", String.Empty, 0);
            GetFoliageTypeResponse response = await GetRequest<GetFoliageTypeResponse>(request);
            return response;
        }

        public async Task<GetMaterialResponse> GetMaterialByType(long typeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetMaterialsByType", "materialTypeId", typeId);
            GetMaterialResponse response = await GetRequest<GetMaterialResponse>(request);
            return response;
        }

        public async Task<GetMaterialResponse> GetMaterials()
        {
            GenericGetRequest request = new GenericGetRequest("GetMaterials", String.Empty, 0);
            GetMaterialResponse response = await GetRequest<GetMaterialResponse>(request);
            return response;
        }

        public async Task<GetMaterialResponse> GetMaterialsByType(long typeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetMaterialsByType", "materialTypeId", typeId);
            GetMaterialResponse response = await GetRequest<GetMaterialResponse>(request);
            return response;
        }

        public async Task<GetMaterialNameResponse> GetMaterialNamesByType(long typeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetMaterialNamesByType", "materialTypeId", typeId);
            GetMaterialNameResponse response = await GetRequest<GetMaterialNameResponse>(request);
            return response;
        }

        public async Task<GetFoliageResponse> GetFoliage()
        {
            GenericGetRequest request = new GenericGetRequest("GetFoliage", String.Empty, 0);
            GetFoliageResponse response = await GetRequest<GetFoliageResponse>(request);
            return response;
        }

        public async Task<GetFoliageResponse> GetFoliageByType(long typeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetFoliageByType", "foliageTypeId", typeId);
            GetFoliageResponse response = await GetRequest<GetFoliageResponse>(request);
            return response;
        }

        public async Task<GetFoliageNameResponse> GetFoliageNamesByType(long typeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetFoliageNamesByType", "foliageTypeId", typeId);
            GetFoliageNameResponse response = await GetRequest<GetFoliageNameResponse>(request);
            return response;
        }

        public async Task<GetPlantResponse> GetPlants()
        {
            GenericGetRequest request = new GenericGetRequest("GetPlants", String.Empty, 0);
            GetPlantResponse response = await GetRequest<GetPlantResponse>(request);
            return response;
        }
 
        public async Task<GetPlantTypeResponse> GetPlantTypes()
        {
            GenericGetRequest request = new GenericGetRequest("GetPlantTypes", String.Empty, 0);
            GetPlantTypeResponse response = await GetRequest<GetPlantTypeResponse>(request);
            return response;
        }

        public async Task<GetPlantNameResponse> GetPlantNamesByType(long typeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetPlantNamesByType", "plantTypeId", typeId);
            GetPlantNameResponse response = await GetRequest<GetPlantNameResponse>(request);
            return response;
        }

        public async Task<GetPlantResponse> GetPlantsByType(long typeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetPlantsByType", "plantTypeId", typeId);
            GetPlantResponse response = await GetRequest<GetPlantResponse>(request);
            return response;
        }

        public async Task<GetContainerResponse> GetContainers()
        {
            GenericGetRequest request = new GenericGetRequest("GetContainers", String.Empty, 0);
            GetContainerResponse response = await GetRequest<GetContainerResponse>(request);
            return response;
        }


        public async Task<List<ContainerNameDTO>> GetContainerNamesByType(long typeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetContainerNamesByType", "containerTypeId", typeId);
            List<ContainerNameDTO> response = await GetRequest<List<ContainerNameDTO>>(request);
            return response;
        }

 

        public async Task<GetContainerTypeResponse> GetContainerTypes()
        {
            GenericGetRequest request = new GenericGetRequest("GetContainerTypes", String.Empty, 0);
            GetContainerTypeResponse response = await GetRequest<GetContainerTypeResponse>(request);
            return response;
        }

        public async Task<GetContainerResponse> GetContainersByType(long typeId)
        {
            GenericGetRequest request = new GenericGetRequest("GetContainersByType", "containerTypeId", typeId);
            GetContainerResponse response = await GetRequest<GetContainerResponse>(request);
            return response;
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
