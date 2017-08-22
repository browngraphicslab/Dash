using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class BezierConverter : IValueConverter
    {
        public BezierConverter(FrameworkElement element1, FrameworkElement element2, FrameworkElement toElement)
        {
            Element1 = element1;
            Element2 = element2;
            ToElement = toElement;
            _figure = new PathFigure();
            _bezier = new BezierSegment();
            _figure.Segments.Add(_bezier);
            _col.Add(_figure);

            Pos2 = Element1.TransformToVisual(ToElement)
                .TransformPoint(new Point(Element1.ActualWidth / 2, Element1.ActualHeight / 2));
        }
        public FrameworkElement Element1 { get; set; }
        public FrameworkElement Element2 { get; set; }
        public FrameworkElement ToElement { get; set; }

        public FrameworkElement Temp1 { get; set; }
        public FrameworkElement Temp2 { get; set; }

        public Point Pos2 { get; set; }
        private PathFigureCollection _col = new PathFigureCollection();
        private PathFigure _figure;
        private BezierSegment _bezier;
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var pos1 = Element1.TransformToVisual(ToElement)
                .TransformPoint(new Point(Element1.ActualWidth / 2, Element1.ActualHeight / 2));

            var pos2 = Element2?.TransformToVisual(ToElement)
                           .TransformPoint(new Point(Element2.ActualWidth / 2, Element2.ActualHeight / 2)) ?? Pos2;

            double offset = Math.Abs((pos1.X - pos2.X) / 3);
            if (pos1.X < pos2.X)
            {
                _figure.StartPoint = new Point(pos1.X + Element1.ActualWidth / 2, pos1.Y);
                _bezier.Point1 = new Point(pos1.X + offset, pos1.Y);
                _bezier.Point2 = new Point(pos2.X - offset, pos2.Y);
                _bezier.Point3 = new Point(pos2.X - (Element2?.ActualWidth / 2 ?? 0), pos2.Y);
            }
            else
            {
                _figure.StartPoint = new Point(pos1.X - Element1.ActualWidth / 2, pos1.Y);
                _bezier.Point1 = new Point(pos1.X - offset, pos1.Y);
                _bezier.Point2 = new Point(pos2.X + offset, pos2.Y);
                _bezier.Point3 = new Point(pos2.X + (Element2?.ActualWidth / 2 ?? 0), pos2.Y);
            }
            return _col;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
