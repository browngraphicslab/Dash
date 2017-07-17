using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Converters
{
    public class StringToDoubleConverter : SafeDataToXamlConverter<double, string>
    {
        private double _number;

        public StringToDoubleConverter(double number)
        {
            _number = number;
        }

        public override string ConvertDataToXaml(double data, object parameter = null)
        {
            _number = data;
            return _number.ToString();
        }

        public override double ConvertXamlToData(string xaml, object parameter = null)
        {
            double coordinateValue;
            if (!double.TryParse(xaml, out coordinateValue))
            {
                coordinateValue = 0;
            }
            _number = coordinateValue;
            return _number;
        }
    }
}
