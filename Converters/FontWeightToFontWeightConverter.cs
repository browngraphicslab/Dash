using System.Diagnostics;
using DashShared;

namespace Dash
{
    public class FontWeightToFontWeightConverter : SafeDataToXamlConverter<FontWeight, Windows.UI.Text.FontWeight>
    {
        public static FontWeightToFontWeightConverter Instance;

        static FontWeightToFontWeightConverter()
        {
            Instance = new FontWeightToFontWeightConverter();
        }

        public override Windows.UI.Text.FontWeight ConvertDataToXaml(FontWeight data, object parameter = null)
        {
            Debug.Assert(data != null);
            return new Windows.UI.Text.FontWeight
            {
                Weight = data.Weight
            };
        }

        public override FontWeight ConvertXamlToData(Windows.UI.Text.FontWeight xaml, object parameter = null)
        {
            return new FontWeight(xaml.Weight);
        }
    }
}
