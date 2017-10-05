using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class BoolToBrushConverter : SafeDataToXamlConverter<bool, SolidColorBrush>
    {
        public override SolidColorBrush ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? new SolidColorBrush(Colors.Transparent) : new SolidColorBrush(Colors.Gray);
        }

        public override bool ConvertXamlToData(SolidColorBrush xaml, object parameter = null)
        {
            return xaml == new SolidColorBrush(Colors.Transparent);
        }
    }
}
