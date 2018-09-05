using System;
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
    public class PointToCoordinateConverter : SafeDataToXamlConverter<Point, double>
    {
        bool _y;
        public PointToCoordinateConverter(bool y) { _y = y;  }
        public override double ConvertDataToXaml(Point data, object parameter = null)
        {
            return _y ? data.Y : data.X;
        }
        public override Point ConvertXamlToData(double xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
