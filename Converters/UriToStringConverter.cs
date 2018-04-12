using System;

namespace Dash
{
    public class UriToStringConverter : SafeDataToXamlConverter<Uri, string>
    {
        public override string ConvertDataToXaml(Uri data, object parameter = null)
        {
            return data.AbsolutePath;
        }

        public override Uri ConvertXamlToData(string xaml, object parameter = null)
        {
            return new Uri(xaml);
        }
    }
}
