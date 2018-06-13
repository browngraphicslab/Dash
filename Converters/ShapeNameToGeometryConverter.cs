using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Dash.Converters
{
    public class ShapeNameToGeometryConverter : SafeDataToXamlConverter<string, Geometry>
    {
        public override Geometry ConvertDataToXaml(string data, object parameter = null)
        {
            var switchDictionary = new Dictionary<BackgroundShape.AdornmentShape, Geometry>
            {
                [BackgroundShape.AdornmentShape.Rectangular] = new RectangleGeometry { Rect = new Rect(0, 0, 1, 1) },
                [BackgroundShape.AdornmentShape.Elliptical] = new EllipseGeometry { Center = new Point(1, 1), RadiusX = 1, RadiusY = 1 },
                [BackgroundShape.AdornmentShape.RoundedRectangle] = CustomGroupGeometryTemplate.RoundedRectangle(),
                [BackgroundShape.AdornmentShape.RoundedFrame] = CustomGroupGeometryTemplate.RoundedFrame(),
                [BackgroundShape.AdornmentShape.Pentagonal] = CustomGroupGeometryTemplate.LinearPolygon(5),
                [BackgroundShape.AdornmentShape.Hexagonal] = CustomGroupGeometryTemplate.LinearPolygon(6),
                [BackgroundShape.AdornmentShape.Octagonal] = CustomGroupGeometryTemplate.LinearPolygon(8),
                [BackgroundShape.AdornmentShape.CustomPolygon] = CustomGroupGeometryTemplate.LinearPolygon(15),
                [BackgroundShape.AdornmentShape.Clover] = CustomGroupGeometryTemplate.Clover(),
            };

            var selectedShape = Enum.TryParse(data, out BackgroundShape.AdornmentShape shapeType) ? switchDictionary[shapeType] : new RectangleGeometry();
            return selectedShape;
        }

        public override string ConvertXamlToData(Geometry xaml, object parameter = null)
        {
            var switchDictionary = new Dictionary<Type, string>
            {
                [typeof(RectangleGeometry)] = BackgroundShape.AdornmentShape.Rectangular.ToString(),
                [typeof(EllipseGeometry)] = BackgroundShape.AdornmentShape.Elliptical.ToString(),
                [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.RoundedRectangle.ToString(),
                [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.RoundedFrame.ToString(),
                [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.Pentagonal.ToString(),
                [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.Hexagonal.ToString(),
                [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.Octagonal.ToString(),
                [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.CustomPolygon.ToString(),
                [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.Clover.ToString(),
            };
            return switchDictionary.TryGetValue(xaml.GetType(), out var selectedTag) ? selectedTag : "Rectangular";
        }
    }
}
