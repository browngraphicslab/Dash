using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class BoolToBrushConverter : SafeDataToXamlConverter<bool, SolidColorBrush>
    {
        public override SolidColorBrush ConvertDataToXaml(bool data, object parameter = null)
        {
            System.Diagnostics.Debug.WriteLine("i'm in here trying to convert shit"); 
            return data ? Application.Current.Resources["WindowsBlue"] as SolidColorBrush : new SolidColorBrush(Colors.Transparent);
        }

        public override bool ConvertXamlToData(SolidColorBrush xaml, object parameter = null)
        {
            return xaml == new SolidColorBrush(Colors.Transparent);
        }
    }
}
