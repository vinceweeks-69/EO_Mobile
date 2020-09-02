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
        PersonAndAddressDTO  Person;
        List<CustomerContainerDTO> customerContainers;
        ObservableCollection<CustomerContainerDTO> customerContainersOC;
        
        public CustomerContainerPage(PersonAndAddressDTO person)
        {
            Person = person;

            InitializeComponent();

            FirstName.Text = person.Person.first_name;
            LastName.Text = person.Person.last_name;
            Phone.Text = person.Person.phone_primary;

            //get data - bind to list view
            customerContainers = new List<CustomerContainerDTO>();

            LoadCustomerContainerData();
        }

        private void LoadCustomerContainerData()
        {
            customerContainers = ((App)App.Current).GetCustomerContainers(Person.Person.person_id).CustomerContainers;

            foreach (CustomerContainerDTO cc in customerContainers)
            {
                customerContainersOC.Add(cc);
            }

            CustomerContainerListView.ItemsSource = customerContainersOC;
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

        private void ViewCustomerContainerImage_Clicked(object sender, EventArgs e)
        {

        }

        private void Camera_Clicked(object sender, EventArgs e)
        {
            StartCamera();
        }

        private void Save_Clicked(object sender, EventArgs e)
        {
            if(String.IsNullOrEmpty(Label.Text))
            {
                DisplayAlert("Error","To save a customer container, you must have a label value","OK");
            }
            else
            {
                string errorMsg = String.Empty;

                List<EOImgData> images = ((App)App.Current).GetImageData();

                //save the customer container object first - if successful, save image, if successful, update customer container with newly minted image id
                CustomerContainerRequest request = new CustomerContainerRequest();
                request.CustomerContainer.CustomerId = Person.Person.person_id;
                request.CustomerContainer.Label = Label.Text;

                ApiResponse response = ((App)App.Current).AddUpdateCustomerContainers(request);

                if(response.Success && images != null && images.Count > 0)
                {
                    AddImageRequest imageRequest = new AddImageRequest();
                    imageRequest.imgBytes = images[0].imgData;
                    ApiResponse imageResponse =  ((App)App.Current).AddImage(imageRequest).Result;

                    if(imageResponse.Success)
                    {
                        request.CustomerContainer.CustomerContainerId = response.Id;
                        request.CustomerContainer.ImageId = imageResponse.Id;

                        response = ((App)App.Current).AddUpdateCustomerContainers(request);

                        if(!response.Success)
                        {
                            errorMsg += "Error updating customer container record with image data. \n";
                        }
                    }
                    else
                    {
                        errorMsg += "Error adding image data for customer container record. \n";
                    }
                }
                else
                {
                    errorMsg += "Error add customer container record. \n";
                }

                if(!String.IsNullOrEmpty(errorMsg))
                {
                    DisplayAlert("Error", errorMsg, "OK");
                }
                else
                {
                    LoadCustomerContainerData();
                }
            }
        }

        private void DeleteCustomerContainer_Clicked(object sender, EventArgs e)
        {

        }
    }
}