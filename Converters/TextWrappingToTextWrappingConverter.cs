using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Converters
{
    public class TextWrappingToTextWrappingConverter : SafeDataToXamlConverter<TextWrapping, Windows.UI.Xaml.TextWrapping>
    {
        public static TextWrappingToTextWrappingConverter Instance;

        static TextWrappingToTextWrappingConverter()
        {
            Instance = new TextWrappingToTextWrappingConverter();
        }

        public override Windows.UI.Xaml.TextWrapping ConvertDataToXaml(TextWrapping data, object parameter = null)
        {
            switch (data)
            {
                case TextWrapping.NoWrap:
                    return Windows.UI.Xaml.TextWrapping.NoWrap;
                case TextWrapping.Wrap:
                    return Windows.UI.Xaml.TextWrapping.Wrap;
                case TextWrapping.WrapWholeWords:
                    return Windows.UI.Xaml.TextWrapping.WrapWholeWords;
                default:
                    throw new ArgumentOutOfRangeException(nameof(data), data, null);
            }
        }

        public override TextWrapping ConvertXamlToData(Windows.UI.Xaml.TextWrapping xaml, object parameter = null)
        {
            switch (xaml)
            {
                case Windows.UI.Xaml.TextWrapping.NoWrap:
                    return TextWrapping.NoWrap;
                case Windows.UI.Xaml.TextWrapping.Wrap:
                    return TextWrapping.Wrap;
                case Windows.UI.Xaml.TextWrapping.WrapWholeWords:
                    return TextWrapping.WrapWholeWords;
                default:
                    throw new ArgumentOutOfRangeException(nameof(xaml), xaml, null);
            }
        }
    }
}
