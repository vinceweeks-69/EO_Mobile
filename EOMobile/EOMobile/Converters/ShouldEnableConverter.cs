using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace EOMobile.Converters
{
    public class ShouldEnableConverter : IValueConverter
    {
        bool shouldEnable; 
        public ShouldEnableConverter(bool shouldEnable)
        {
            this.shouldEnable = shouldEnable;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)shouldEnable;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)shouldEnable;
        }
    }
}
