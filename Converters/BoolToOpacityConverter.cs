using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class BoolToOpacityConverter : SafeDataToXamlConverter<bool, double>
    {
        public override double ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? 1 : 0;
        }

        public override bool ConvertXamlToData(double opacity, object parameter = null)
        {
            return opacity > 0;
        }
    }
}
