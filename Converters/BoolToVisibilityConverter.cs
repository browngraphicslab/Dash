using System;
using Windows.UI.Xaml;

namespace Dash
{
    public class BoolToVisibilityConverter : SafeDataToXamlConverter<bool, Visibility>
    {
        public override Visibility ConvertDataToXaml(bool data, object parameter = null)
        {
            if (parameter != null)
            {
                return (data || (bool)parameter) ? Visibility.Visible : Visibility.Collapsed;
            }
            return data ? Visibility.Visible : Visibility.Collapsed;
        }

        public override bool ConvertXamlToData(Visibility xaml, object parameter = null)
        {
            return xaml == Visibility.Visible;
        }
    }
}
