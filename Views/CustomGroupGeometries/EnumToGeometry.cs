using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public static class EnumToGeometry
    {
        public static Dictionary<BackgroundShape.AdornmentShape, Func<Geometry>> GetDict = new Dictionary<BackgroundShape.AdornmentShape, Func<Geometry>>
        {
            [BackgroundShape.AdornmentShape.Rectangular] = () => new RectangleGeometry { Rect = new Rect(0, 0, 1, 1) },
            [BackgroundShape.AdornmentShape.Elliptical] = () => new EllipseGeometry { Center = new Point(1, 1), RadiusX = 1, RadiusY = 1 },
            [BackgroundShape.AdornmentShape.RoundedRectangle] = CustomGroupGeometryTemplate.RoundedRectangle,
            [BackgroundShape.AdornmentShape.RoundedFrame] = CustomGroupGeometryTemplate.RoundedFrame,
            [BackgroundShape.AdornmentShape.Pentagonal] = () => CustomGroupGeometryTemplate.LinearPolygon(5),
            [BackgroundShape.AdornmentShape.Hexagonal] = () => CustomGroupGeometryTemplate.LinearPolygon(6),
            [BackgroundShape.AdornmentShape.Octagonal] = () => CustomGroupGeometryTemplate.LinearPolygon(8),
            [BackgroundShape.AdornmentShape.Clover] = CustomGroupGeometryTemplate.Clover,
        };
    }
}
