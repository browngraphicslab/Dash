using Windows.UI.Xaml;

namespace Dash
{
    public class BoolToVisibilityConverter : SafeDataToXamlConverter<bool, Visibility>
    {
        public override Visibility ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? Visibility.Visible : Visibility.Collapsed;
        }

        public override bool ConvertXamlToData(Visibility xaml, object parameter = null)
        {
            return xaml == Visibility.Visible;
        }
    }
    public class BoolToNumberConverter : SafeDataToXamlConverter<bool, double>
    {
        public override double ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? 1 : 0;
        }

        public override bool ConvertXamlToData(double xaml, object parameter = null)
        {
            return xaml != 0;
        }
    }
}
