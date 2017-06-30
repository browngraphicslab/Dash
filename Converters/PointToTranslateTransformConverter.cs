using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class PointToTranslateTransformConverter : SafeDataToXamlConverter<Point, TranslateTransform>
    {
        public override TranslateTransform ConvertDataToXaml(Point data, object parameter = null)
        {
            return new TranslateTransform
            {
                X = data.X,
                Y = data.Y
            };
        }

        public override Point ConvertXamlToData(TranslateTransform xaml, object parameter = null)
        {
            return new Point(xaml.X, xaml.Y);
        }

        public static PointToTranslateTransformConverter Instance;

        static PointToTranslateTransformConverter()
        {
            Instance = new PointToTranslateTransformConverter();
        }
    }
}
