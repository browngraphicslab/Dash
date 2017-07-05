using System;
using System.Diagnostics;
using System.Globalization;
using Windows.Foundation;

namespace Dash
{
    /// <summary>
    /// Converter which allows the user to make bindings to one coordinate in a <see cref="Point"/>
    /// </summary>
    public class StringCoordinateToPointConverter : SafeDataToXamlConverter<Point, string>
    {
        private Point _point;

        public StringCoordinateToPointConverter(Point initialPoint)
        {
            _point = initialPoint;
        }

        public override string ConvertDataToXaml(Point data, object parameter = null)
        {
            _point = data;
            Debug.Assert(parameter is Coordinate);
            var coordinate = (Coordinate) parameter;
            switch (coordinate)
            {
                case Coordinate.X:
                    return data.X.ToString(CultureInfo.InvariantCulture);
                case Coordinate.Y:
                    return data.Y.ToString(CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override Point ConvertXamlToData(string xaml, object parameter = null)
        {
            Debug.Assert(parameter is Coordinate);
            var coordinate = (Coordinate)parameter;

            double coordinateValue;
            if (!double.TryParse(xaml, out coordinateValue))
            {
                coordinateValue = 0;
            }

            switch (coordinate)
            {
                case Coordinate.X:
                    _point = new Point(coordinateValue, _point.Y);
                    return _point;
                case Coordinate.Y:
                    _point = new Point(_point.X, coordinateValue);
                    return _point;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum Coordinate
    {
        X, Y
    }
}
