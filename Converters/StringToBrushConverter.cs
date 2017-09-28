
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Dash.Converters
{
    class StringToColorConverter : SafeDataToXamlConverter<string, Brush>
    {
        public override Brush ConvertDataToXaml(string data, object parameter = null)
        {
            return (Brush)XamlBindingHelper.ConvertValue(typeof(Brush), data);
        }

        public override string ConvertXamlToData(Brush data, object parameter = null)
        {
            return (string)XamlBindingHelper.ConvertValue(typeof(string), (data as SolidColorBrush)?.Color ?? Colors.White);
        }
    }
}