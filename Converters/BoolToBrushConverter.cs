using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class BoolToBrushConverter : SafeDataToXamlConverter<bool, SolidColorBrush>
    {
        SolidColorBrush _brushTrue,  _brushFalse;
        public BoolToBrushConverter(SolidColorBrush brushTrue, SolidColorBrush brushFalse)
        {
            _brushFalse = brushFalse;
            _brushTrue = brushTrue;
        }
        public BoolToBrushConverter()
        {
            _brushFalse = new SolidColorBrush(Colors.Transparent);
            _brushTrue = Application.Current.Resources["WindowsBlue"] as SolidColorBrush;
        }
        public override SolidColorBrush ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ?  _brushTrue : _brushFalse;
        }

        public override bool ConvertXamlToData(SolidColorBrush xaml, object parameter = null)
        {
            return xaml == _brushFalse;
        }
    }
}
