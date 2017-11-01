using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Converters
{
    public class StringToDoubleConverter : SafeDataToXamlConverter<double, string>
    {
        double _minValue = double.NaN, _maxValue = double.NaN;
        public StringToDoubleConverter(double minValue = double.NaN, double maxValue = double.NaN)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override string ConvertDataToXaml(double data, object parameter = null)
        {
            return data.ToString();
        }

        public override double ConvertXamlToData(string xaml, object parameter = null)
        {
            if (!double.TryParse(xaml, out double outputValue))
            {
                outputValue = 0;
            }
            return clamp(outputValue);
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