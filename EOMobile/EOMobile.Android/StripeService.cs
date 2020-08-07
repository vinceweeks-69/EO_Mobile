using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stripe;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace EOMobile.Droid
{
    //public class StripeServices : IStripeServices
    //{
    //    //public string CardToToken(CreditCard creditCard)
    //    //{
    //    //    var stripeTokenCreateOptions = new StripeTokenCreateOptions
    //    //    {
    //    //        Card = new StripeCreditCardOptions
    //    //        {
    //    //            Number = creditCard.Numbers,
    //    //            ExpirationMonth = Int32.Parse(creditCard.Month),
    //    //            ExpirationYear = Int32.Parse(creditCard.Year),
    //    //            Cvc = creditCard.Cvc,
    //    //            Name = creditCard.HolderName
    //    //        }
    //    //    };

    //    //    var tokenService = new StripeTokenService();
    //    //    var stripeToken = tokenService.Create(stripeTokenCreateOptions);

    //    //    return stripeToken.Id;
    //    //}
    //}
}