using Windows.UI.Text;
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

    public class InverseBoolToVisibilityConverter : SafeDataToXamlConverter<bool, Visibility>
    {
        public override Visibility ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? Visibility.Collapsed : Visibility.Visible;
        }

        public override bool ConvertXamlToData(Visibility xaml, object parameter = null)
        {
            return xaml == Visibility.Collapsed;
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

    //bcz: temporary hack --- need to just convert bool to Bold for use anywhere, not just SchemaHeaders
    public class BoolToBoldConverter : SafeDataToXamlConverter<bool, FontWeight>
    {
        public override FontWeight ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? FontWeights.ExtraBold : FontWeights.Normal;
        }

        public override bool ConvertXamlToData(FontWeight xaml, object parameter = null)
        {
            throw new System.Exception();
            //return xaml.Equals(FontWeights.ExtraBold);
        }
    }
}
