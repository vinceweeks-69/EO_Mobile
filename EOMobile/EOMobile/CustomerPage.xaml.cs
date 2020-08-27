using Android.App;
using EO.ViewModels.ControllerModels;
using EOMobile.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Runtime.CompilerServices;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CustomerPage : EOBasePage
	{
        List<PersonAndAddressDTO> customerList = new List<PersonAndAddressDTO>();
        ObservableCollection<PersonAndAddressDTO> list1 = new ObservableCollection<PersonAndAddressDTO>();
        ContentPage Initiator;

        public CustomerPage ()
		{
			InitializeComponent ();

            GetAllCustomers();

            CustomerListView.ItemSelected += CustomerListView_ItemSelected;

            State.ItemsSource = ((App)App.Current).GetStateNames();

            History.IsEnabled = false;
		}

        public CustomerPage(ContentPage initiator) : this()
        {
            Initiator = initiator;
        }

        private void ClearForm()
        {
            FirstName.Text = String.Empty;
            LastName.Text = String.Empty;
            Phone.Text = String.Empty;
            Email.Text = String.Empty;
            Address.Text = String.Empty;
            Address2.Text = String.Empty;
            City.Text = String.Empty;
            State.SelectedIndex = -1;
            Zip.Text = String.Empty;

            History.IsEnabled = false;
        }
        private void CustomerListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ListView lv = sender as ListView;

            if (lv != null)
            {
                PersonAndAddressDTO item = lv.SelectedItem as PersonAndAddressDTO;

                if (item != null)
                {
                    FirstName.Text = item.Person.first_name;
                    LastName.Text = item.Person.last_name;
                    Phone.Text = item.Person.phone_primary;
                    Email.Text = item.Person.email;
                    Address.Text = item.Address != null ? item.Address.street_address : String.Empty;
                    Address2.Text = item.Address != null ? item.Address.unit_apt_suite : String.Empty;
                    City.Text = item.Address != null ? item.Address.city : String.Empty;
                    State.SelectedItem = item.Address != null ? item.Address.state : String.Empty;
                    Zip.Text = item.Address != null ? item.Address.zipcode : String.Empty;

                    History.IsEnabled = true;
                }
            }
        }

        void GetAllCustomers()
        {
            ClearForm();

            customerList = ((App)App.Current).GetCustomers(new GetPersonRequest());

            list1.Clear();

            foreach (PersonAndAddressDTO p in customerList)
            {
                list1.Add(p);
            }

            CustomerListView.ItemsSource = list1;
        }
        public void OnShowAllCustomersClicked(object sender, EventArgs e)
        {
            GetAllCustomers();
        }

        public void OnCustomerSearchClicked(object sender, EventArgs e)
        {
            GetPersonRequest request = new GetPersonRequest();

            request.Email = Email.Text;
            request.FirstName = FirstName.Text;
            request.LastName = LastName.Text;
            request.PhonePrimary = Phone.Text;

            customerList = ((App)App.Current).GetCustomers(request);

            list1.Clear();

            foreach (PersonAndAddressDTO p in customerList)
            {
                list1.Add(p);
            }

            CustomerListView.ItemsSource = list1;
        }

        public void OnSaveCustomerClicked(object sender, EventArgs e)
        {
            AddCustomerRequest request = new AddCustomerRequest();

            if (!String.IsNullOrEmpty(FirstName.Text) && !String.IsNullOrEmpty(LastName.Text) &&
                !String.IsNullOrEmpty(Phone.Text) && !String.IsNullOrEmpty(Email.Text))
            {
                request.Customer.Person.first_name = FirstName.Text;
                request.Customer.Person.last_name = LastName.Text;
                request.Customer.Person.email = Email.Text;
                request.Customer.Person.phone_primary = Phone.Text;

                request.Customer.Address.street_address = Address.Text;
                request.Customer.Address.unit_apt_suite = Address2.Text;
                request.Customer.Address.city = City.Text;
                if ((State as Picker).SelectedItem != null)
                {
                    request.Customer.Address.state = State.SelectedItem.ToString();
                }
                request.Customer.Address.zipcode = Zip.Text;

                SaveCustomer(request);
            }
            else
            {
                DisplayAlert("Error", "To save a customer, enter at least First and Last Name, Phone and Email", "Ok");
            }
        }

        private void SaveCustomer(AddCustomerRequest request)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);
                //client.DefaultRequestHeaders.Add("appkey", "myapp_key");
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", ((App)(App.Current)).User + " : " + ((App)(App.Current)).Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = client.PostAsync("api/Login/AddCustomer", content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    //strReader.Close();
                    ApiResponse apiResponse = JsonConvert.DeserializeObject<ApiResponse>(strData);

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
                        if (Initiator != null && Initiator as WorkOrderPage != null)
                        {
                            request.Customer.Person.person_id = apiResponse.Id;

                            MessagingCenter.Send<PersonAndAddressDTO>(request.Customer, "SearchCustomer");

                            Navigation.PopModalAsync();
                        }    
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
                Exception ex2 = new Exception("AddCustomer", ex);
                ((App)App.Current).LogError(ex2.Message, JsonConvert.SerializeObject(request));
            }

        }

        public void OnDeleteCustomerClicked(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                long itemId = Int64.Parse(button.CommandParameter.ToString());
                var c = list1.Where(a => a.Person.person_id == itemId).First();

                list1.Remove(c);
            }
        }

        private void Clear_Clicked(object sender, EventArgs e)
        {
            FirstName.Text = String.Empty;
            LastName.Text = String.Empty;
            Phone.Text = String.Empty;
            Email.Text = String.Empty;
            Address.Text = String.Empty;
            Address2.Text = String.Empty;
            City.Text = String.Empty;
            State.SelectedIndex = -1;
            Zip.Text = String.Empty;

        }

        private void Help_CustomerPage_Clicked(object sender, EventArgs e)
        {
            TaskAwaiter t = Navigation.PushAsync(new HelpPage("CustomerPage")).GetAwaiter();
        }

        private void History_Clicked(object sender, EventArgs e)
        {

        }

        private void Containers_Clicked(object sender, EventArgs e)
        {
            TaskAwaiter t = Navigation.PushAsync(new CustomerContainerPage()).GetAwaiter();
        }
    }
}