using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Converters
{
    class StringToEnumConverter<TEnum> : SafeDataToXamlConverter<string, TEnum> where TEnum : struct, IComparable, IFormattable, IConvertible

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
