using Newtonsoft.Json;
using SharedData;
using Stripe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using ViewModels.DataModels;

namespace EOMobile
{
    public interface IStripeServices
    {
        Task<CardValidate> CardToToken(CreditCard creditCard);

        Charge MakePayment(string token, decimal salePrice, string buyer);
    }
   
    public class StripeServices : IStripeServices
    {
        public StripeServices()
        {
            //StripeConfiguration.ApiKey = "pk_test_qEqBdPz6WTh3CNdcc9bgFXpz00haS1e8hC";

            StripeConfiguration.ApiKey = "sk_test_6vJyMV6NxHArGV6kI2EL6R7V00kzjXJ72u";
        }

        public PaymentResponse MakeStripePayment(CreditCard creditCard)
        {
            PaymentResponse response = new PaymentResponse();

            try
            {
                CardInput cc = new CardInput();

                cc.cardNumber = creditCard.Numbers;
                cc.expirationDate = new DateTime(Convert.ToInt32(creditCard.Year), Convert.ToInt32(creditCard.Month), 1).ToShortDateString();
                cc.cvc = creditCard.Cvc;
                cc.amount = 50;
                cc.customerName = "test buyer";

                string hash = JsonConvert.SerializeObject(cc);
                byte[] hash1 = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };
                byte[] hash2 = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

                PaymentRequest request = new PaymentRequest();

                byte[] wtf = Encryption.EncryptStringToBytes(hash, hash1, hash2);
                request.payload = wtf;

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
                }
                else
                {
                    //MessageBox.Show("There was an error retreiving plants");
                }
            }
            catch (Exception ex)
            {
                int debug = 1;
            }

            return response;
        }

        public async Task<CardValidate> CardToToken(CreditCard creditCard)
        {
            CardValidate ccValidate = new CardValidate();

            try
            {
                var options = new TokenCreateOptions
                {
                    Card = new CreditCardOptions
                    {
                        Number = creditCard.Numbers,
                        ExpYear = Convert.ToInt64(creditCard.Year),
                        ExpMonth = Convert.ToInt64(creditCard.Month),
                        Cvc = creditCard.Cvc,
                        //Currency = "usd"
                    }
                };

                var tokenService = new TokenService();

                Token stripeToken = tokenService.CreateAsync(options).Result;

                ccValidate.ccConfirm = stripeToken.Id;
            }
            catch (Exception ex)
            {
                string stackTrace = Environment.StackTrace;

                StripeException stripeException = ex.InnerException as StripeException;

                if (stripeException != null)
                {
                    ccValidate.ErrorMessages.Add(HandleStripeException(stripeException));
                }
            }

            return ccValidate;
        }

        public Charge MakePayment(string token, decimal salePrice, string buyer)
        {
            Charge c = new Charge();

            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt64(salePrice),
                Currency = "usd",
                Description = "Charge for " + buyer + " on " + Convert.ToString(DateTime.Now),
                Source = token
            };

            try
            {
                var service1 = new ChargeService();

                c = service1.Create(options);
            }
            catch(Exception ex)
            {
                StripeException stripeException = ex.InnerException as StripeException;

                if (stripeException != null)
                {
                    string errorMsg = HandleStripeException(stripeException);
                }
            }

            return c;
        }

        private string HandleStripeException(StripeException ex)
        {
            string errorMsg = String.Empty;

            Dictionary<String, String>  stripeErrorDictionary = new Dictionary<String, String>() {
                { "invalid_number", "The card number is not a valid credit card number." },
                { "invalid_expiry_month", "The card's expiration month is invalid." },
                { "invalid_expiry_year", "The card's expiration year is invalid." },
                { "invalid_cvc", "The card's security code is invalid." },
                { "invalid_swipe_data", "The card's swipe data is invalid." },
                { "incorrect_number", "The card number is incorrect." },
                { "expired_card", "The card has expired." },
                { "incorrect_cvc", "The card's security code is incorrect." },
                { "incorrect_zip", "The card's zip code failed validation." },
                { "card_declined", "The card was declined." },
                { "missing", "There is no card on a customer that is being charged." },
                { "processing_error", "An error occurred while processing the card." },
            };

            if (stripeErrorDictionary.ContainsKey(ex.StripeError.Code))
            {
                errorMsg = stripeErrorDictionary[ex.StripeError.Code];
            }
            else
            {
                errorMsg = "An unknown error occurred.";
            }

            return errorMsg;
        }
    }


    public class CreditCard
    {
        private static Regex regex = new Regex("^[0-9]+$", RegexOptions.Compiled);
        public string Numbers { get; set; }
        public string HolderName { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string Cvc { get; set; }
        public string token { get; set; }

        /// <summary>
        /// Initializes a new instance of the CreditCard class.
        /// </summary>
        public CreditCard()
        {
            Numbers = "";
            Month = "";
            Year = "";
            Cvc = "";
            HolderName = "";
            token = "";
        }

        /// <summary>
        /// Verifies the credit card info. 
        /// However, if the data provided aren't matching an existing card, 
        /// it will still return `true` since that function only checks the basic template of a credit card data.
        /// </summary>
        /// <returns>True if the card data match the basic card information. False otherwise</returns>
        public List<string> VerifyCreditCardInfo()
        {
            List<string> messages = new List<string>();

            if (Numbers == ""
                || Month == ""
                || Year == ""
                || Cvc == ""
                || HolderName == "")
            {
                messages.Add("please enter your payment details");
                return messages;
            }
            try
            {
                if(!regex.IsMatch(Numbers) || Numbers.Length != 16)
                {
                    messages.Add("The credit card number must be 16 digits");
                }

                int month = 0;
                int year = 0;
                int cvc = 0;

                if (!Int32.TryParse(Month, out month)
                    || !Int32.TryParse(Year, out year)
                    || !Int32.TryParse(Year, out cvc))
                {
                    messages.Add("please add expiration date and/or security code.");
                }    

                if (month < 1 || month > 12)
                {
                    messages.Add("the expiration month date must be between 1 and 12");
                }

                if (year < DateTime.Now.Year)
                {
                    messages.Add("the expiration year must be in the future, or the current year IF the expiration month is in the future.");
                }  
                
                if(year == DateTime.Now.Year && month < DateTime.Now.Month)
                {
                    messages.Add("the expiration month/year must be in the future.");
                }

                if (year >  DateTime.Now.Year + 20)
                {
                    messages.Add("the expiration year must be in the future, but not TOO far in the future.");
                }

                if (!regex.IsMatch(Cvc) ||  Cvc.Length != 3)
                {
                    messages.Add("the security code must be three digits.");
                }
            }
            catch (Exception)
            {
                //log?
            }

            return messages;
        }
    }
}
