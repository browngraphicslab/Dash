using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class ShapeInformationToGeometryConverter : SafeDataToXamlConverter<List<object>, Geometry>
    {
        public override Geometry ConvertDataToXaml(List<object> data, object parameter = null)
        {
            var type = (string) data[0];
            if (!Enum.TryParse(type, out BackgroundShape.AdornmentShape shapeType)) return new RectangleGeometry();

            var numSides = (int) (double) data[1];
            if (shapeType == BackgroundShape.AdornmentShape.CustomPolygon) return CustomGroupGeometryTemplate.LinearPolygon(numSides);
            if (shapeType == BackgroundShape.AdornmentShape.CustomStar) return CustomGroupGeometryTemplate.Star(numSides);
            return EnumToGeometry.GetDict[shapeType]();
        }

        public override List<object> ConvertXamlToData(Geometry xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
