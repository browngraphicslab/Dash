using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public override Windows.UI.Text.FontWeight ConvertDataToXaml(FontWeight data)
        {
            Debug.Assert(data != null);
            return new Windows.UI.Text.FontWeight
            {
                Weight = data.Weight
            };
        }

        public override FontWeight ConvertXamlToData(Windows.UI.Text.FontWeight xaml)
        {
            return new FontWeight(xaml.Weight);
        }
    }
}
