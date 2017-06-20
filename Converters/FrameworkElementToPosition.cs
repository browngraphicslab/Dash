using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public class FrameworkElementToPosition : IValueConverter
    {
        private bool _useX;

        public FrameworkElementToPosition(bool useX)
        {
            _useX = useX;
        }

        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            var kv = (KeyValuePair<FrameworkElement, FrameworkElement>)parameter;
            FrameworkElement element = kv.Key;
            FrameworkElement toElement = kv.Value;
            Point p = element.TransformToVisual(toElement)
                .TransformPoint(new Point(element.Width / 2, element.Height / 2));
            return _useX ? p.X : p.Y;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }
}