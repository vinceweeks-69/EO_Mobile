using Newtonsoft.Json;
using SharedData;
using System;
using System.Collections.Generic;
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
    public partial class PaymentPage : EOBasePage
    {
        string ccConfirm = String.Empty;
        long workOrderId = 0;
        long workOrderPaymentId = 0;
        public bool useGiftCard = false;

        List<PersonAndAddressDTO> buyer = new List<PersonAndAddressDTO>();
        WorkOrderResponse workOrder;
        GetWorkOrderSalesDetailResponse workOrderSalesDetail;

        List<WorkOrderInventoryItemDTO> workOrderInventoryList;
        List<KeyValuePair<int, string>> discountType = new List<KeyValuePair<int, string>>();
        List<KeyValuePair<long, string>> payTypeList = new List<KeyValuePair<long, string>>();

        public PaymentPage()
        {
            InitializeComponent();

            CCFrame.IsVisible = false;

            PaySuccess.IsVisible = false;

            discountType.Add(new KeyValuePair<int, string>(0, "None"));
            discountType.Add(new KeyValuePair<int, string>(1, "Percent"));
            discountType.Add(new KeyValuePair<int, string>(2, "Manual"));

            DiscountType.ItemsSource = discountType;
            DiscountType.SelectedIndex = 0;

            payTypeList.Add(new KeyValuePair<long, string>(1, "Cash"));
            payTypeList.Add(new KeyValuePair<long, string>(2, "Check"));
            payTypeList.Add(new KeyValuePair<long, string>(3, "Credit Card"));

            PaymentType.ItemsSource = payTypeList;
            PaymentType.SelectedIndex = 0;

            DiscountType.SelectedItem = 0;
            DiscountType.SelectedIndex = 0;

            GiftCardNumberLabel.Text = "";
            GiftCardNumber.IsVisible = false;

            GiftCardAmountLabel.Text = "";
            GiftCardAmount.IsVisible = false;

        }
        public PaymentPage(long workOrderId, List<WorkOrderInventoryItemDTO> workOrderInventoryList) : this()
        {

            this.workOrderId = workOrderId;
            this.workOrderInventoryList = workOrderInventoryList;

            ((App)App.Current).GetWorkOrder(workOrderId).ContinueWith(a => WorkOrderLoaded(a.Result));
        }

        public PaymentPage(WorkOrderResponse workOrder, WorkOrderPaymentDTO payment) : this()
        {
            SetPaymentData(payment);
        }

        private void SetPaymentData(WorkOrderPaymentDTO payment)
        {
            Pay.IsEnabled = false;
           
            PaymentType.SelectedIndex = payment.WorkOrderPaymentType;
            PaymentType.IsEnabled = false;

            CCFrame.IsVisible = false;

            if (payment.WorkOrderPaymentType == 2)
            {
                ccConfirmLabel.IsVisible = true;
                ccConfirmPaid.IsVisible = true;
                ccConfirmPaid.Text = payment.WorkOrderPaymentCreditCardConfirmation;
               
            }

            DiscountType.SelectedIndex = payment.DiscountType;
            DiscountType.IsEnabled = false;

            DiscountAmount.IsEnabled = false;

            if (payment.DiscountType > 0)
            {
                DiscountAmount.Text = payment.DiscountAmount.ToString("N2");
            }

            if (!String.IsNullOrEmpty(payment.GiftCardNumber))
            {
                GiftCardNumber.Text = payment.GiftCardNumber;
                GiftCardNumber.IsEnabled = false;
            }


            if (payment.GiftCardAmount > 0)
            {
                GiftCard.IsChecked = true;
                GiftCard_PropertyChanged(GiftCard, null);
                GiftCard.IsEnabled = false;
                GiftCardAmount.Text = payment.GiftCardAmount.ToString("N2");
                GiftCardAmount.IsEnabled = false;
            }

            SubTotal.Text = "";
            SubTotal.IsEnabled = false;

            Tax.Text = payment.WorkOrderPaymentTax.ToString("N2");
            Tax.IsEnabled = false;

            Total.Text = payment.WorkOrderPaymentAmount.ToString("N2");
            Total.IsEnabled = false;
        }

        private void WorkOrderLoaded(WorkOrderResponse response)
        {
            if(response.WorkOrder.CustomerId != 0)
            {
                workOrder = response;

                GetPersonRequest personRequest = new GetPersonRequest()
                {
                    PersonId = workOrder.WorkOrder.CustomerId
                };

                ((App)App.Current).GetCustomers(personRequest).ContinueWith(a => BuyerLoaded(a.Result));
            }
        }

        private void BuyerLoaded(GetPersonResponse response)
        {
            buyer = response.PersonAndAddress;

            WorkOrderResponse request = new WorkOrderResponse();
            request.WorkOrderList = workOrder.WorkOrderList;
            request.NotInInventory = workOrder.NotInInventory;
            request.Arrangements = workOrder.Arrangements;

            ((App)App.Current).PostRequest<WorkOrderResponse, GetWorkOrderSalesDetailResponse>("GetWorkOrderDetail", request).ContinueWith(a => WorkOrderDetailLoaded(a.Result));
        }

        private void WorkOrderDetailLoaded(GetWorkOrderSalesDetailResponse response)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                workOrderSalesDetail = response;

                //set subtotal and tax fields
                SubTotal.Text = response.SubTotal.ToString("N2");

                Tax.Text = response.Tax.ToString("N2");

                Total.Text = response.Total.ToString("N2");

                SetWorkOrderSalesData();
            });
        }

        //Event handler is being called multiple times and will be called after this function
        //we want default useGiftCard to equal false - set it to true here to get the desired result
        protected override void OnAppearing()
        {
            base.OnAppearing();

            //useGiftCard = false;
            //GiftCard.IsChecked = false;

            //GiftCardNumberLabel.Text = "";
            //GiftCardNumber.IsVisible = false;

            //GiftCardAmountLabel.Text = "";
            //GiftCardAmount.IsVisible = false;
        }

        private void PaymentType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Picker p = sender as Picker;

            if (p != null)
            {
                if (p.SelectedIndex == 2)
                {
                    CCFrame.IsVisible = true;
                }
                else
                {
                    CCFrame.IsVisible = false;
                }
            }
        }

        private void DiscountType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Picker p = sender as Picker;

            if (p != null)
            {
                if (p.SelectedIndex > 0)
                {
                    DiscountAmountLabel.Text = "Discount Amount";
                    DiscountAmount.IsVisible = true;
                    DiscountAmount.IsEnabled = true;
                }
                else
                {
                    DiscountAmountLabel.Text = String.Empty;
                    DiscountAmount.IsVisible = false;
                    DiscountAmount.IsEnabled = false;
                    DiscountAmount.Text = String.Empty;

                    if(Pay.IsEnabled)
                    {
                        Total.Text = SetWorkOrderSalesData();
                    }
                }
            }
        }
        
        private void Pay_Clicked(object sender, EventArgs e)
        {
            Pay.IsEnabled = false;

            string ccConfirm = String.Empty;

            if (PaymentType.SelectedIndex == 2)
            {
                PayWithCC();
            }
            else
            {
                bool paymentSaved = SavePaymentRecord(ccConfirm).Result;

                if(paymentSaved)
                {
                    PaymentSuccess();
                }
            }
        }

        private void PayWithCC()
        {
            Pay.IsEnabled = false;

            CreditCard cc = new CreditCard()
            {
                Cvc = CVV.Text,
                HolderName = NameOnCard.Text,
                Numbers = CardNumber.Text,
                Month = ExpirationMonth.Text,
                Year = ExpirationYear.Text
            };

            List<string> msgs = cc.VerifyCreditCardInfo();

            if (msgs.Count == 0)
            {
                try
                {
                    StripeServices stripe = new StripeServices();

                    CardValidate ccValidate = stripe.CardToToken(cc).Result;

                    if (!String.IsNullOrEmpty(ccValidate.ccConfirm))
                    {
                        cc.token = ccValidate.ccConfirm;

                        string strMoneyValue = Total.Text;
                        decimal decimalMoneyValue = decimal.Parse(strMoneyValue, NumberStyles.Currency);
                        PaymentResponse response = MakeStripePayment(cc, decimalMoneyValue);

                        if (response.success)
                        {
                            //what should we do if the card is successfully charged but the payment record isn't saved?
                            bool paymentSaved = SavePaymentRecord(response.StripeChargeId).Result;

                            if (paymentSaved)
                            {
                                PaymentSuccess();
                            }
                        }
                        else
                        {
                            //add message let them try again? Send email?
                            DisplayAlert("Error", "Payment Unsuccessful", "OK");
                            Pay.IsEnabled = true;
                        }
                    }
                    else
                    {
                        DisplayAlert("Error", ccValidate.ErrorMessages[0], "OK");
                        Pay.IsEnabled = true;
                    }
                }
                catch(Exception ex)
                {
                    //CANNOT save cc data to db
                    Exception ex2 = new Exception("PayWithCC", ex);
                    ((App)App.Current).LogError(ex2.Message, "Card holder name is " + NameOnCard.Text);
                }
            }
            else
            {
                string errorMsg = String.Empty;
                foreach (string msg in msgs)
                {
                    errorMsg += msg + "\n";
                }

                //ErrorMessages.Text = errorMsg;
                Pay.IsEnabled = true;
            }
        }

        void PaymentSuccess()
        {
            EmailReceipt();

            PaySuccess.IsVisible = true;
            PaymentSuccessMessages.Text = "Payment Successful";
        }

        private void EmailReceipt()
        {
            try
            {
                if (buyer.Count == 1)
                {
                    if (!String.IsNullOrEmpty(buyer[0].Person.email))
                    {
                        ((App)App.Current).GetWorkOrderPayment(workOrderId).ContinueWith(a => WorkOrderPaymentLoaded(a.Result));
                    }
                    else //let EO know the customer needs to add an email address
                    {
                        EmailHelpers emailHelper = new EmailHelpers();

                        EOMailMessage mailMessage = new EOMailMessage();

                        string emailHtml = emailHelper.ComposeMissingEmail(buyer[0]);

                        mailMessage = new EOMailMessage("service@elegantorchids.com", "information@elegantorchids.com", "Missing Customer Email", emailHtml, "Orchids@5185");

                        Email.SendEmail(mailMessage);
                    }
                }
            }
            catch(Exception ex)
            {

            }
        }

        private async void WorkOrderPaymentLoaded(WorkOrderPaymentDTO payment)
        {
            GenericGetRequest request = new GenericGetRequest("GetWorkOrderPrices", "workOrderId", payment.WorkOrderId);
            ((App)App.Current).GetRequest<GetWorkOrderPriceResponse>(request).ContinueWith(a => WorkOrderPricesLoaded(workOrder,payment,a.Result));
        }

        private void WorkOrderPricesLoaded(WorkOrderResponse workOrder, WorkOrderPaymentDTO payment, GetWorkOrderPriceResponse workOrderPriceResponse)
        {
            EmailHelpers emailHelper = new EmailHelpers();

            EOMailMessage mailMessage = new EOMailMessage();

            string emailHtml = emailHelper.ComposeReceipt(workOrder, payment, workOrderPriceResponse);

            mailMessage = new EOMailMessage("service@elegantorchids.com", buyer[0].Person.email, "Elegant Orchids Receipt", emailHtml, "Orchids@5185");

            Email.SendEmail(mailMessage);
        }

        private void PaySuccess_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send<WorkOrderResponse>(workOrder,"PaymentSuccess");

            Navigation.PopAsync();
        }

        void PaymentFailure()
        {
            DisplayAlert("Error", "Payment Unsuccessful", "OK");

            Pay.IsEnabled = true;
        }

        private PaymentResponse MakeStripePayment(CreditCard creditCard, decimal salePrice)
        {
            PaymentResponse response = new PaymentResponse();

            try
            {
                CardInput cc = new CardInput();

                cc.nameonCard = creditCard.HolderName;
                ////testing 4242424242424242
                cc.cardNumber = creditCard.Numbers;
                cc.expirationDate = new DateTime(Convert.ToInt32(creditCard.Year), Convert.ToInt32(creditCard.Month), 1).ToShortDateString();
                cc.cvc = creditCard.Cvc;
                cc.amount = salePrice;
                cc.customerName = creditCard.HolderName;

                string hash = JsonConvert.SerializeObject(cc);

                PaymentRequest request = new PaymentRequest();

                Random rnd = new Random();
                request.test = rnd.Next(32, 64);

                request.payload = Encryption.EncryptStringToBytes(hash, Encryption.GetBytes(Encryption.StatsOne(request.test)), Encryption.GetBytes(Encryption.StatsTwo(request.test)));

                cc.token = creditCard.token;

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(((App)App.Current).LAN_Address);

                client.DefaultRequestHeaders.Accept.Add(
                   new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("EO-Header", ((App)App.Current).User + " : " + ((App)App.Current).Pwd);

                string jsonData = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = client.PostAsync("api/Login/MakeStripePayment", content).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    Stream streamData = httpResponse.Content.ReadAsStreamAsync().Result;
                    StreamReader strReader = new StreamReader(streamData);
                    string strData = strReader.ReadToEnd();
                    response = JsonConvert.DeserializeObject<PaymentResponse>(strData);

                    if (response.success)
                    {
                        MessagingCenter.Send<PaymentResponse>(response, "CCPayment");
                    }
                }
                else
                {
                    //MessageBox.Show("There was an error processing the credit card payment.");
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = new Exception("MakeStripePayment", ex);
                ((App)App.Current).LogError(ex2.Message, "Card holder name is " + creditCard.HolderName);
            }

            return response;
        }

        private WorkOrderPaymentDTO GetWorkOrderPaymentDTO()
        {
            WorkOrderPaymentDTO workOrderPayment = new WorkOrderPaymentDTO();
            decimal workValue = 0;
            workOrderPayment.WorkOrderId = workOrderId;
            decimal.TryParse(Total.Text, NumberStyles.Currency, CultureInfo.CurrentCulture.NumberFormat, out workValue);
            workOrderPayment.WorkOrderPaymentAmount = workValue;
            workValue = 0;
            workOrderPayment.WorkOrderPaymentType = (int)payTypeList[PaymentType.SelectedIndex].Key;
            workOrderPayment.WorkOrderPaymentCreditCardConfirmation = ccConfirm;
            decimal.TryParse(Tax.Text, NumberStyles.Currency, CultureInfo.CurrentCulture.NumberFormat, out workValue);
            workOrderPayment.WorkOrderPaymentTax = workValue;
            workOrderPayment.DiscountType = DiscountType.SelectedIndex;

            if (workOrderPayment.DiscountType > 0)
            {
                workValue = 0;
                decimal.TryParse(DiscountAmount.Text, out workValue);
                workOrderPayment.DiscountAmount = workValue;
            }

            workValue = 0;
            decimal.TryParse(GiftCardAmount.Text, out workValue);

            if (workValue > 0)
            {
                workOrderPayment.GiftCardAmount = workValue;
                workOrderPayment.GiftCardNumber = GiftCardNumber.Text;
            }

            return workOrderPayment;
        }

        private async Task<bool> SavePaymentRecord(string ccConfirm)
        {
            bool success = false;

            WorkOrderPaymentDTO workOrderPayment = GetWorkOrderPaymentDTO();

            workOrderPayment.WorkOrderPaymentCreditCardConfirmation = ccConfirm;

            workOrderPaymentId = ((App)App.Current).AddWorkOrderPayment(workOrderPayment);

            if (workOrderPaymentId > 0)
            {
                MarkWorkOrderPaidRequest request = new MarkWorkOrderPaidRequest(workOrderPayment.WorkOrderId);

                success = ((App)App.Current).MarkWorkOrderPaid(request);
            }

            return success;
        }

        private void GiftCard_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(useGiftCard)
            {
                GiftCardNumberLabel.Text = "Gift Card Number";
                GiftCardNumber.IsVisible = true;

                GiftCardAmountLabel.Text = "Gift Card Amount";
                GiftCardAmount.IsVisible = true;
            }
            else
            {
                GiftCardNumberLabel.Text = "";
                GiftCardNumber.IsVisible = false;

                GiftCardAmountLabel.Text = "";
                GiftCardAmount.IsVisible = false;
                GiftCardNumber.Text = String.Empty;
                GiftCardAmount.Text = String.Empty;

                if (Pay.IsEnabled) // they might have turn gift card on, set a value (total affected) and then turned gift card back off
                {
                    Total.Text = SetWorkOrderSalesData();
                }
            }

            useGiftCard = !useGiftCard;
        }

        private void Back_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        private void Help_PaymentPage_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(PaymentPage)))
            {
                TaskAwaiter t = Navigation.PushAsync(new HelpPage("PaymentPage")).GetAwaiter();
            }
        }

        private string SetWorkOrderSalesData()
        {
            decimal total = 0.0M;
            decimal tax = 0.0M;

            if(!String.IsNullOrEmpty(SubTotal.Text))
            {
                total = Convert.ToDecimal(SubTotal.Text);
            }

            if (!String.IsNullOrEmpty(Tax.Text))
            {
                tax = Convert.ToDecimal(Tax.Text);
                total += tax;
            }

            if (!String.IsNullOrEmpty(DiscountAmount.Text))
            {
                if (DiscountType.SelectedIndex == 1)  //percent 
                {
                    decimal temp = Convert.ToDecimal(DiscountAmount.Text);
                    if (temp != 0 && temp < 100)
                    {
                        total -= total * (temp / 100);
                    }
                }
                else
                {
                    total -= Convert.ToDecimal(DiscountAmount.Text);
                }
            }

            if (!String.IsNullOrEmpty(GiftCardAmount.Text))
            {
                total -= Convert.ToDecimal(GiftCardAmount.Text);
            }

            return total.ToString("N2");
        }

        private void DiscountAmount_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Pay.IsEnabled)
            {
                Total.Text = SetWorkOrderSalesData();
            }
        }

        private void GiftCardAmount_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Pay.IsEnabled)
            {
                Total.Text = SetWorkOrderSalesData();
            }
        }
    }
}