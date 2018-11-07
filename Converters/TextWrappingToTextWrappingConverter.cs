using System;
using DashShared;
using Windows.UI.Xaml;

namespace Dash.Converters
{
    public class StringToTextWrappingConverter : SafeDataToXamlConverter<string,  TextWrapping>
    {
        public static StringToTextWrappingConverter Instance;

        static StringToTextWrappingConverter()
        {
            Instance = new StringToTextWrappingConverter();
        }

        public override  TextWrapping ConvertDataToXaml(string data, object parameter = null)
        {
            if (data ==  TextWrapping.NoWrap.ToString())
                return  TextWrapping.NoWrap;
            if (data ==  TextWrapping.Wrap.ToString())
                return  TextWrapping.Wrap;
            if (data == TextWrapping.WrapWholeWords.ToString())
                return TextWrapping.WrapWholeWords;
            throw new ArgumentOutOfRangeException(nameof(data), data, null);
        }

        public override string ConvertXamlToData(TextWrapping xaml, object parameter = null)
        {
            switch (xaml)
            {
                case  TextWrapping.NoWrap:
                    return TextWrapping.NoWrap.ToString();
                case  TextWrapping.Wrap:
                    return TextWrapping.Wrap.ToString();
                case  TextWrapping.WrapWholeWords:
                    return TextWrapping.WrapWholeWords.ToString();
                default:
                    throw new ArgumentOutOfRangeException(nameof(xaml), xaml, null);
            }
        }
    }
}
