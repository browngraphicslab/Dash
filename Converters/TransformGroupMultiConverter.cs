using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Dash
{
    //Pos, Scale
    class TransformGroupMultiConverter : SafeDataToXamlConverter<List<object>, MatrixTransform>
    {
        public override MatrixTransform ConvertDataToXaml(List<object> data, object parameter = null)
        {
            return ConvertDataToXamlHelper(data, parameter); 
        }

        public static MatrixTransform ConvertDataToXamlHelper(List<object> data, object parameter = null)
        {
            var group = new TransformGroup();
            if (data.Count > 0 && data[0] is Point pos)
            {
                if (data.Count > 1 && data[1] is Point scale)
                {
                    group.Children.Add(new ScaleTransform()
                    {
                        CenterX = 0,
                        CenterY = 0,
                        ScaleX  = scale.X,
                        ScaleY  = scale.Y
                    });
                }
                group.Children.Add(new TranslateTransform()
                {
                    X = pos.X,
                    Y = pos.Y
                });
            }
            return new MatrixTransform() { Matrix = group.Value };
        }

        public override List<object> ConvertXamlToData(MatrixTransform xaml, object parameter = null)
        {
            var val = xaml.Matrix;
            //TODO This technically doesn't get the scale correctly if there is a rotation applied
            return new List<object>{new Point(val.OffsetX, val.OffsetY), new Point(val.M11, val.M22)};
        }
    }
}
