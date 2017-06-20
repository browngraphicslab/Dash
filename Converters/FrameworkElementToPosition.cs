using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;

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
           // Debug.WriteLine($"{(element.DataContext as Key).Name}: {(element.RenderTransform as MatrixTransform).Matrix}, {(toElement.RenderTransform as MatrixTransform).Matrix}, {(element.TransformToVisual(toElement) as MatrixTransform).Matrix}");
            return _useX ? p.X : p.Y;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }
}