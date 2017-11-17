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
        double _minValue = double.NaN, _maxValue = double.NaN;
        public DoubleToStringConverter(double minValue=double.NaN, double maxValue=double.NaN)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return "" + value; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            double n;
            if (double.TryParse((string)value, out n))
                return clamp(n);
            return clamp(0);
        }

        private double clamp(double n)
        {
            if (!double.IsNaN(_minValue) && n < _minValue)
                return _minValue;
            if (!double.IsNaN(_maxValue) && n > _maxValue)
                return _maxValue;
            return n;
        }
    }
}
