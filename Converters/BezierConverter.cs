﻿using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class BezierConverter : IValueConverter
    {
        public delegate void OnPathUpdatedEventHandler(BezierConverter converter);

        public event OnPathUpdatedEventHandler OnPathUpdated;

        public BezierConverter(FrameworkElement element1, FrameworkElement element2, FrameworkElement toElement)
        {
            Element1 = element1;
            Temp1 = element1;
            Element2 = element2;
            Temp2 = element2;
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

        public LinearGradientBrush GradientBrush { get; set; }

        public void setGradientAngle() // TODO: remove all references to this
        {

            var pos1 = Element1.TransformToVisual(ToElement)
                .TransformPoint(new Point(Element1.ActualWidth / 2, Element1.ActualHeight / 2));

            var pos2 = Element2?.TransformToVisual(ToElement)
                           .TransformPoint(new Point(Element2.ActualWidth / 2, Element2.ActualHeight / 2)) ?? Pos2;

            var g = new GradientStopCollection();
            g.Add(new GradientStop() { Color = ((SolidColorBrush)App.Instance.Resources["OutputHandleColor"]).Color, Offset = 0 });
            g.Add(new GradientStop() { Color = ((SolidColorBrush)App.Instance.Resources["InputHandleColor"]).Color, Offset = 1 });

            if (pos1.X > pos2.X)
            {
                g[0].Offset = 1;
                g[1].Offset = 0;
            }
            else
            {
                g[0].Offset = 0;
                g[1].Offset = 1;
            }
            
                GradientBrush = new LinearGradientBrush(g, 0);
        }

        public Point Pos2 { get; set; }
        private PathFigureCollection _col = new PathFigureCollection();
        private PathFigure _figure;
        private BezierSegment _bezier;
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            UpdateLine();
            setGradientAngle();
            OnPathUpdated?.Invoke(this);
            return _col;
        }

        public void UpdateLine()
        {
            var pos1 = Util.PointTransformFromVisual(new Point(Element1.ActualWidth / 2, Element1.ActualHeight / 2), Element1, ToElement);
            var pos2 = Element2?.TransformToVisual(ToElement).TransformPoint(new Point(Element2.ActualWidth / 2, Element2.ActualHeight / 2)) ?? Pos2;

            double offset = Math.Abs((pos1.X - pos2.X) / 3);
            if (pos1.X < pos2.X)
            {
                _figure.StartPoint = Util.PointTransformFromVisual(new Point(Element1.ActualWidth, Element1.ActualHeight / 2), Element1, ToElement);
                _bezier.Point1 = new Point(pos1.X + offset, pos1.Y);
                _bezier.Point2 = new Point(pos2.X - offset, pos2.Y);
                if (Element2 == null) _bezier.Point3 = pos2;
                else _bezier.Point3 = Util.PointTransformFromVisual(new Point(0, Element2.ActualHeight / 2), Element2, ToElement);
            }
            else
            {
                _figure.StartPoint = Util.PointTransformFromVisual(new Point(0, Element1.ActualHeight / 2), Element1, ToElement);
                _bezier.Point1 = new Point(pos1.X - offset, pos1.Y);
                _bezier.Point2 = new Point(pos2.X + offset, pos2.Y);
                if (Element2 == null) _bezier.Point3 = pos2;
                else _bezier.Point3 = Util.PointTransformFromVisual(new Point(Element2.ActualWidth, Element2.ActualHeight / 2), Element2, ToElement);
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
