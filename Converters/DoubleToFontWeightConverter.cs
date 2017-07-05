using Windows.UI.Text;

namespace Dash.Converters
{
    public class DoubleToFontWeightConverter : SafeDataToXamlConverter<double, FontWeight>
    {
        public override FontWeight ConvertDataToXaml(double data, object parameter = null)
        {
            return new FontWeight {Weight = (ushort) data};
        }

        public override double ConvertXamlToData(FontWeight xaml, object parameter = null)
        {
            return xaml.Weight;
        }
    }
}
