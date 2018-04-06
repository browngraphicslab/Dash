using System;

namespace Dash.Converters
{
    class StringToEnumConverter<TEnum> : SafeDataToXamlConverter<string, TEnum> where TEnum : struct, IComparable, IFormattable

    {
        public override TEnum ConvertDataToXaml(string data, object parameter = null)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), data);
        }

        public override string ConvertXamlToData(TEnum xaml, object parameter = null)
        {
            return xaml.ToString();
        }
    }
}
