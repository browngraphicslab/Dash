
using System;
using Windows.UI;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Dash.Converters
{
    internal class StringToBrushConverter : SafeDataToXamlConverter<string, Brush>
    {
        public override Brush ConvertDataToXaml(string data, object parameter = null)
        {
            //return (Brush)XamlBindingHelper.ConvertValue(typeof(Brush), data);
            try
            {
                return (Brush)XamlBindingHelper.ConvertValue(typeof(Brush), data);
            }
            catch (Exception)
            {
                return new SolidColorBrush(Color.FromArgb(128, 117, 165, 148));
            }
        }

        public override string ConvertXamlToData(Brush data, object parameter = null)
        {
            return (string)XamlBindingHelper.ConvertValue(typeof(string), (data as SolidColorBrush)?.Color ?? Colors.White);
        }
    }
}