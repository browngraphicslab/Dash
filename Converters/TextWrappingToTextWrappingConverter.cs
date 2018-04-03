using System;
using DashShared;

namespace Dash.Converters
{
    public class StringToTextWrappingConverter : SafeDataToXamlConverter<string, Windows.UI.Xaml.TextWrapping>
    {
        public static StringToTextWrappingConverter Instance;

        static StringToTextWrappingConverter()
        {
            Instance = new StringToTextWrappingConverter();
        }

        public override Windows.UI.Xaml.TextWrapping ConvertDataToXaml(string data, object parameter = null)
        {
            if (data ==  TextWrapping.NoWrap.ToString())
                return Windows.UI.Xaml.TextWrapping.NoWrap;
            if (data ==  TextWrapping.Wrap.ToString())
                return Windows.UI.Xaml.TextWrapping.Wrap;
            if (data == TextWrapping.WrapWholeWords.ToString())
                return Windows.UI.Xaml.TextWrapping.WrapWholeWords;
            throw new ArgumentOutOfRangeException(nameof(data), data, null);
        }

        public override string ConvertXamlToData(Windows.UI.Xaml.TextWrapping xaml, object parameter = null)
        {
            switch (xaml)
            {
                case Windows.UI.Xaml.TextWrapping.NoWrap:
                    return TextWrapping.NoWrap.ToString();
                case Windows.UI.Xaml.TextWrapping.Wrap:
                    return TextWrapping.Wrap.ToString();
                case Windows.UI.Xaml.TextWrapping.WrapWholeWords:
                    return TextWrapping.WrapWholeWords.ToString();
                default:
                    throw new ArgumentOutOfRangeException(nameof(xaml), xaml, null);
            }
        }
    }
}
