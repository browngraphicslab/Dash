using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using Microsoft.Toolkit.Uwp.UI.Extensions;

namespace Dash.Converters
{
    class ShapeNameToShapeConverter : SafeDataToXamlConverter<String, Shape>
    {
        public override Shape ConvertDataToXaml(string data, object parameter = null)
        {
            Shape shape = null;
            switch (data)
            {
                case "Elliptical":
                    shape = new Ellipse();
                    break;
                case "Rectangular":
                    shape = new Rectangle();
                    break;
                case "Rounded":
                    Shape innerRectangle = new Rectangle();
                    (innerRectangle as Rectangle).RadiusX = (innerRectangle as Rectangle).RadiusY = 40;
                    shape = innerRectangle;
                    break;
            }

            if (shape != null)
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
