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
            var selectedShape = shapeType == BackgroundShape.AdornmentShape.CustomPolygon ? CustomGroupGeometryTemplate.LinearPolygon((int)(double)data[1]) : EnumToGeometry.GetDict[shapeType]();
            return selectedShape;
        }

        public override List<object> ConvertXamlToData(Geometry xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
