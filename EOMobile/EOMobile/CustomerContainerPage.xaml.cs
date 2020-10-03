using Rg.Plugins.Popup.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomerContainerPage : EOBasePage
    {
        bool forWorkOrder;
        long selectedCustomerContainerId = 0;
        long selectedCustomerContainerImageId = 0;
        PersonAndAddressDTO  Person;
        List<CustomerContainerDTO> customerContainers;
        ObservableCollection<CustomerContainerDTO> customerContainersOC;
        
        public CustomerContainerPage(PersonAndAddressDTO person, bool forWorkOrder = false)
        {
            Person = person;

            this.forWorkOrder = forWorkOrder;
            InitializeComponent();

            FirstName.Text = person.Person.first_name;
            LastName.Text = person.Person.last_name;
            Phone.Text = person.Person.phone_primary;

            //get data - bind to list view
            customerContainers = new List<CustomerContainerDTO>();
            customerContainersOC = new ObservableCollection<CustomerContainerDTO>();

            LoadCustomerContainerData();
        }

        private async void LoadCustomerContainerData()
        {
            ((App)App.Current).GetCustomerContainers(Person.Person.person_id).ContinueWith(a => ShowCustomerContainerData(a.Result));
        }

        private void ShowCustomerContainerData(CustomerContainerResponse response)
        {
            customerContainers = response.CustomerContainers;
            customerContainersOC.Clear();

            foreach (CustomerContainerDTO cc in customerContainers)
            {
                customerContainersOC.Add(cc);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                CustomerContainerListView.ItemsSource = customerContainersOC;
            });
        }
        
        private void ViewCustomerContainerImage_Clicked(object sender, EventArgs e)
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
                CustomerContainerDTO customerContainer = (CustomerContainerDTO)((Button)sender).BindingContext;

                if (customerContainer != null)
                {
                    long imageId = ((App)App.Current).MissingImageId;
                    if (customerContainer.ImageId != 0)
                    {
                        imageId = customerContainer.ImageId;
                    }

                    EOImgData img = ((App)App.Current).GetImage(imageId);

                    //ServiceCodeDTO serviceCode = ((App)App.Current).GetServiceCodeById(material.Inventory.ServiceCodeId);

                    string price = string.Empty;
                    //if (serviceCode.ServiceCodeId > 0)
                    //{
                    //    price = (serviceCode.Price.HasValue ? serviceCode.Price.Value.ToString("C2", CultureInfo.CurrentCulture) : String.Empty);
                    //}

                    PopupImagePage popup = new PopupImagePage(img, price);

                    Navigation.PushPopupAsync(popup);

                    if (imageId == ((App)App.Current).MissingImageId)
                    {
                        //MessagingCenter.Send<MaterialInventoryDTO>(material, "MaterialMissingImage");
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

        private void Camera_Clicked(object sender, EventArgs e)
        {
            StartCamera();
        }

        private void Save_Clicked(object sender, EventArgs e)
        {
            if (forWorkOrder)
            {
                if (selectedCustomerContainerId != 0)
                {
                    CustomerContainerDTO cc = customerContainersOC.Where(a => a.CustomerContainerId == selectedCustomerContainerId).FirstOrDefault();
                    MessagingCenter.Send<CustomerContainerDTO>(cc, "AddCustomerContainerToWorkOrder");
                    Navigation.PopAsync();
                }
                else
                {
                    DisplayAlert("?", "Please select a customer container", "OK");
                }
            }
            else
            {
                if (String.IsNullOrEmpty(Label.Text))
                {
                    DisplayAlert("Error", "To save a customer container, you must have at least, a label value", "OK");
                }
                else
                {
                    string errorMsg = String.Empty;

                    //save the customer container object first - if successful, save image, if successful, update customer container with newly minted image id
                    CustomerContainerRequest request = new CustomerContainerRequest();
                    request.CustomerContainer.CustomerContainerId = selectedCustomerContainerId;
                    request.CustomerContainer.CustomerId = Person.Person.person_id;
                    request.CustomerContainer.Label = Label.Text;
                    request.CustomerContainer.ImageId = selectedCustomerContainerImageId;
                    ((App)App.Current).AddUpdateCustomerContainers(request).ContinueWith(a => AddCustomerContainerImage(request, a.Result));
                }
            }
        }

        private void AddCustomerContainerImage(CustomerContainerRequest request, ApiResponse response)
        {
            if (response.Success)
            {
                request.CustomerContainer.CustomerContainerId = response.Id;

                List<EOImgData> images = ((App)App.Current).GetImageData();

                if (images != null && images.Count > 0)
                {
                    AddImageRequest imageRequest = new AddImageRequest();
                    imageRequest.imgBytes = images[0].imgData;
                    ((App)App.Current).AddImage(imageRequest).ContinueWith(a => UpdateCustomerContainerWithImage(request, response, a.Result));
                }
                else
                {
                    CustomerContainerSaveComplete(response);
                }
            }
            else
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    DisplayAlert("Error", "Problem saving customer container", "Ok");
                });
            }
        }

        private void UpdateCustomerContainerWithImage(CustomerContainerRequest request, ApiResponse containerResponse, ApiResponse imageResponse)
        {
            //request has customerID - containerResponse has CustomerContainerId, imageResponse has ImageId

            if(imageResponse.Success)
            {
                request.CustomerContainer.ImageId = imageResponse.Id;
                ((App)App.Current).AddUpdateCustomerContainers(request).ContinueWith(a => CustomerContainerSaveComplete(a.Result));
            }
        }

        private void CustomerContainerSaveComplete(ApiResponse response)
        {
            if(response.Success)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    DisplayAlert("Success", "Customer container saved!", "Ok");

                    selectedCustomerContainerId = 0;
                    selectedCustomerContainerImageId = 0;
                    ((App)App.Current).ClearImageData();
                    ((App)App.Current).ClearImageDataList();

                    LoadCustomerContainerData();
                });
            }
        }

        private void DeleteCustomerContainer_Clicked(object sender, EventArgs e)
        {
            CustomerContainerDTO customerContainer = (CustomerContainerDTO)((Button)sender).BindingContext;

            if (customerContainer != null)
            {
                CustomerContainerRequest request = new CustomerContainerRequest();
                request.CustomerContainer.CustomerContainerId = customerContainer.CustomerContainerId;
                request.CustomerContainer.CustomerId = customerContainer.CustomerId;
                ((App)App.Current).DeleteCustomerContainer(request).ContinueWith(a => CustomerContainerSaveComplete(a.Result));
            }
        }

        private void AddCustomerContainerImage_Clicked(object sender, EventArgs e)
        {

        }

        private void CustomerContainerListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            //populate the top part of the form with the selected data and enable the ability to add a new image
            ListView lv = sender as ListView;

            if (lv != null)
            {
                CustomerContainerDTO item = lv.SelectedItem as CustomerContainerDTO;

                if (item != null)
                {
                    selectedCustomerContainerId = item.CustomerContainerId;
                    selectedCustomerContainerImageId = item.ImageId;
                    Label.Text = item.Label;
                }
            }
        }

        private void Clear_Clicked(object sender, EventArgs e)
        {
            Label.Text = String.Empty;
            selectedCustomerContainerId = 0;
            selectedCustomerContainerImageId = 0;
            CustomerContainerListView.SelectedItem = null;
        }
    }
}