using Windows.UI.Text;

namespace Dash.Converters
{
    class StringToFontweightConverter : SafeDataToXamlConverter<string, FontWeight>

    {
        // TODO shitty code bc Fontweights is not an enum change later 

        public override FontWeight ConvertDataToXaml(string data, object parameter = null)
        {
            if (data == "Black")
            {
                return FontWeights.Black;
            }
            else if (data == "Bold")
            {
                return FontWeights.Bold;
            }
            else if (data == "Light")
            {
                return FontWeights.Light;
            }
            return FontWeights.Normal;
        }

        public override string ConvertXamlToData(FontWeight xaml, object parameter = null)
        {
            if (xaml.Weight == FontWeights.Black.Weight)
            {
                return "Black";
            }
            else if (xaml.Weight == FontWeights.Bold.Weight)
            {
                return "Bold";
            }
            else if (xaml.Weight == FontWeights.Light.Weight)
            {
                return "Light";
            }
            return "Normal";
        }
    }
}
