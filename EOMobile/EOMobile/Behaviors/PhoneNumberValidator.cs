using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace EOMobile
{
    public class PhoneNumberValidator : Behavior<Entry>
    {
        public static PhoneNumberValidator Instance = new PhoneNumberValidator();

        ///
        /// Attaches when the page is first created.
        /// 

        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnEntryTextChanged;
            base.OnAttachedTo(entry);
        }

        ///
        /// Detaches when the page is destroyed.
        /// 

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnEntryTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                // If the new value is longer than the old value, the user is
                if (e.OldTextValue != null && e.NewTextValue.Length < e.OldTextValue.Length)
                    return;

                var value = e.NewTextValue;

                if (value.Length == 3)
                {
                    ((Entry)sender).Text += "-";
                    return;
                }

                if (value.Length == 7)
                {
                    ((Entry)sender).Text += "-";
                    return;
                }

                ((Entry)sender).Text = e.NewTextValue;
            }
        }
    }
}
