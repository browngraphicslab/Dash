using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Dash.Converters
{
    class ShapeNameToGeometryConverter : SafeDataToXamlConverter<String, Geometry>
    {
        public override Geometry ConvertDataToXaml(string data, object parameter = null)
        {
            Geometry shape = null;
            switch (data)
            {
                case "Elliptical":
                    shape = new EllipseGeometry();
                    break;
                case "Rectangular":
                    shape = new RectangleGeometry();
                    break;
                case "Rounded":
                    Shape innerRectangle = new Rectangle();
                    (innerRectangle as Rectangle).RadiusX = (innerRectangle as Rectangle).RadiusY = 40;
                    shape = innerRectangle;
                    break;
            }

            if (shape is FrameworkElement uiElement)
            {
                shape.AddFieldBinding(Shape.FillProperty, parameter as FieldBinding<TextController>);
            }

            return shape;
        }

        public override string ConvertXamlToData(Shape xaml, object parameter = null)
        {
            if (xaml.GetType() == typeof(Ellipse))
            {
                return "Elliptical";
            }
            else if (xaml.GetType() == typeof(Rectangle))
            {
                if ((xaml as Rectangle).RadiusX == 40)
                {
                    return "Rounded";
                }

                return "Rectangular";
            }
            else
            {
                Debug.WriteLine("ERROR: This case should not be reached");
                return "none";
            }
        }
    }
}
