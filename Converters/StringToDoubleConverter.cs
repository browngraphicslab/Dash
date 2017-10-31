using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Converters
{
    public class StringToDoubleConverter : SafeDataToXamlConverter<double, string>
    {

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
            return outputValue;
        }
    }
}