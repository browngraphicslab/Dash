using System;
using Windows.UI.Xaml.Data;

namespace Dash
{
    /// <summary>
    /// Converts doubles to booleans and back. 0 = false, 1 = true (or any nonzero number). Used
    /// primarily to convert NumberFieldModels into boolean values.
    /// </summary>
    public class DoubleToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((double)value != 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value) return 1.0;
            return 0.0;
        }
    }
    public class DoubleToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (int)(double)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (double)(int)value;
        }
    }
}
