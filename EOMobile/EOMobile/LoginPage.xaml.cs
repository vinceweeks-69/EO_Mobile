﻿using EO.ViewModels.ControllerModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace EOMobile
{
    public partial class LoginPage : ContentPage
    {
        public string LAN_Address
        {
            get { return ((App)App.Current).LAN_Address; }
        }
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

        public LoginPage()
        {
            InitializeComponent();
        }

        void OnLoginButtonClicked(object sender, EventArgs e)
        {
            string message = String.Empty;
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            Login.IsEnabled = false;

            try
            {
                HttpClient client = new HttpClient();

                //client.Timeout = new TimeSpan(0, 0, 0, 3, 0);

                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);

                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                User = this.Name.Text;
                Pwd = this.Password.Text;

                client.DefaultRequestHeaders.Add("EO-Header", User + " : " + Pwd);

                LoginRequest request = new LoginRequest(User, Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                httpResponse = client.PostAsync("api/Login/Login", content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    IEnumerable<string> values;
                    httpResponse.Headers.TryGetValues("EO-Header", out values);
                    if (values != null && values.ToList().Count == 1)
                    {
                        this.Name.Text = String.Empty;
                        this.Password.Text = String.Empty;
                        //Navigation.PushAsync(new MainPage());
                        Navigation.PushAsync(new DashboardPage());
                    }
                }
                else
                {
                    if(httpResponse.StatusCode == System.Net.HttpStatusCode.Forbidden || 
                        httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        message = "Unrecognized username / password";
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("failed to connect"))
                {
                    message = "Device not connected to network";
                }
            }
            finally
            {
                if (!String.IsNullOrEmpty(message))
                {
                    DisplayAlert("Error", message, "Cancel");
                }
                //else if(httpResponse.StatusCode == System.Net.HttpStatusCode.OK && httpResponse.RequestMessage is null)
                //{
                //    DisplayAlert("Login", "could not connect to server", "Cancel");
                //}

                Login.IsEnabled = true;
            }
        }
    }
}