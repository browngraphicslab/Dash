using Windows.UI.Text;
using Windows.UI.Xaml;
using Dash.Views;

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

    //bcz: temporary hack --- need to just convert bool to Bold for use anywhere, not just SchemaHeaders
    public class BoolToBoldConverter : SafeDataToXamlConverter<CollectionDBSchemaHeader.HeaderViewModel, FontWeight>
    {
        public override FontWeight ConvertDataToXaml(CollectionDBSchemaHeader.HeaderViewModel data, object parameter = null)
        {
            return data.Selected ? FontWeights.ExtraBold : FontWeights.Normal;
        }

        public override CollectionDBSchemaHeader.HeaderViewModel ConvertXamlToData(FontWeight xaml, object parameter = null)
        {
            throw new System.Exception();
            //return xaml.Equals(FontWeights.ExtraBold);
        }
    }
}
