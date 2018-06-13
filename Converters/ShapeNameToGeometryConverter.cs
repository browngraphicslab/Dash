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
            var selectedShape = Enum.TryParse(data, out BackgroundShape.AdornmentShape shapeType) ? EnumToGeometry.GetDict[shapeType]() : new RectangleGeometry();
            return selectedShape;
        }

        public override string ConvertXamlToData(Geometry xaml, object parameter = null)
        {
            throw new NotImplementedException();
            //var switchDictionary = new Dictionary<Type, string>
            //{
            //    [typeof(RectangleGeometry)] = BackgroundShape.AdornmentShape.Rectangular.ToString(),
            //    [typeof(EllipseGeometry)] = BackgroundShape.AdornmentShape.Elliptical.ToString(),
            //    [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.RoundedRectangle.ToString(),
            //    [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.RoundedFrame.ToString(),
            //    [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.Pentagonal.ToString(),
            //    [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.Hexagonal.ToString(),
            //    [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.Octagonal.ToString(),
            //    [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.CustomPolygon.ToString(),
            //    [typeof(PathGeometry)] = BackgroundShape.AdornmentShape.Clover.ToString(),
            //};
            //return switchDictionary.TryGetValue(xaml.GetType(), out var selectedTag) ? selectedTag : "Rectangular";
        }
    }
}
