using Windows.UI.Xaml;

namespace Dash
{
    public class IsNullToVisibilityConverter : SafeDataToXamlConverter<object, Visibility>
    {
        public override Visibility ConvertDataToXaml(object data, object parameter = null)
        {
            return data == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public override object ConvertXamlToData(Visibility xaml, object parameter = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
