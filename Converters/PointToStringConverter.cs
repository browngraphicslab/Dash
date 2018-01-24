using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{
    public class PointToStringConverter : SafeDataToXamlConverter<Point, string>
    {
        private readonly DoubleToStringConverter _doubleToStringConverter;

        public PointToStringConverter()
        {
            _doubleToStringConverter = new DoubleToStringConverter();
        }

        public override string ConvertDataToXaml(Point data, object parameter = null)
        {
            return $"({data.X}, {data.Y})";
        }

        public override Point ConvertXamlToData(string xaml, object parameter = null)
        {

            // remove parentheses and spaces
            xaml = xaml.Replace("(", string.Empty).Replace(")", string.Empty);
            xaml = Regex.Replace(xaml, @"\s", "");

            // split on comma
            var numbers = xaml.Split(",");

            // convert the numbers to strings
            double x = 0;
            if (numbers.Length > 0)
            {
                x = _doubleToStringConverter.ConvertXamlToData(numbers[0]);
            }
            double y = 0;
            if (numbers.Length > 1)
            {
                y = _doubleToStringConverter.ConvertXamlToData(numbers[1]);
            }

            return new Point(x, y);

        }
    }
}
