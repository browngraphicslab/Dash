using System;

namespace Dash
{
    /// <summary>
    /// Converter supports bidirectional conversion between DateTime and string
    /// </summary>
    public class DateTimeToStringConverter : SafeDataToXamlConverter<DateTime, string>
    {
        /*
         * Returns the received DateTime as a string in general long format
         */
        public override string ConvertDataToXaml(DateTime data, object parameter = null)
        {
            return data.ToString("G");
        }

        /*
         * Parses the string in general long format representing a DateTime and attempts to convert it into an instance of DateTime
         * Should always yield a successful conversion given that the current counterpart DateTime to String method returns a string in general long format.
         */
        public override DateTime ConvertXamlToData(string xaml, object parameter = null)
        {
            var dateTime = new DateTime();
            DateTime.TryParse(xaml, out dateTime);
            return dateTime;
        }
    }
}