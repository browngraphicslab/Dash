using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Dash.Converters
{
    class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return "" + value; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            double n;
            if (double.TryParse((string)value, out n)) return n;
            return 0; 
        }
    }
}
