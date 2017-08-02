using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Dash
{
    /// <summary>
    /// Converts doubles to booleans and back. 0 = false, 1 = true (or any nonzero number). Used
    /// primarily to convert NumberFieldModels into boolean values.
    /// </summary>
    public class InverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((Visibility)value == Visibility.Collapsed)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if ((Visibility)value == Visibility.Collapsed)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }
    }
}