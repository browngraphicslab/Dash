using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Dash
{
    /// <summary>
    /// Converter which allows the user to make bindings to one coordinate in a <see cref="Point"/>
    /// </summary>
    public class StringCoordinateToPointConverter : SafeDataToXamlConverter<Point, string>
    {

        /// <summary>
        /// The <see cref="Coordinate"/> which this converter is bound to
        /// </summary>
        public Coordinate BoundCoordinate { get; }

        /// <summary>
        /// Create a new <see cref="StringCoordinateToPointConverter"/> which is bound to the <paramref name="coordinateToBind"/>
        /// </summary>
        /// <param name="coordinateToBind"></param>
        public StringCoordinateToPointConverter(Coordinate coordinateToBind)
        {
            BoundCoordinate = coordinateToBind;
        }

        /// <summary>
        /// Converts a <see cref="Point"/> into the <see cref="double"/> matching the <see cref="BoundCoordinate"/>
        /// </summary>
        /// <param name="data">The transform which is being converted</param>
        /// <param name="parameter">The parameter is unused</param>
        public override string ConvertDataToXaml(Point data, object parameter = null)
        {
            switch (BoundCoordinate)
            {
                case Coordinate.X:
                    return data.X.ToString(CultureInfo.InvariantCulture);
                case Coordinate.Y:
                    return data.Y.ToString(CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Converts a <see cref="double"/> into a <see cref="Point"/>. The <paramref name="xaml"/> is the double
        /// representing the <see cref="BoundCoordinate"/>, the <paramref name="parameter"/> is a <see cref="Windows.Foundation.Point"/>
        /// which represents the entire position the <see cref="Point"/> is supposed to be set to
        /// </summary>
        /// <param name="xaml">The double which is being converted</param>
        /// <param name="parameter">A <see cref="Windows.Foundation.Point"/></param>
        public override Point ConvertXamlToData(string xaml, object parameter = null)
        {
            Debug.Assert(parameter is Func<Point>, "the parameter must be the point representing the full position of the TranslateTransform you want to beind to");
            var point = ((Func<Point>) parameter)();
            return point;
        }
    }

    public enum Coordinate
    {
        X, Y
    }
}
