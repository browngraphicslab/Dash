using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Dash
{
    class TransformGroupDataToGroupTransformConverter : SafeDataToXamlConverter<TransformGroupData, TransformGroup>
    {
        public override TransformGroup ConvertDataToXaml(TransformGroupData data, object parameter = null)
        {
            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform
            {
                CenterX = data.ScaleCenter.X,
                CenterY = data.ScaleCenter.Y,
                ScaleX = data.ScaleAmount.X,
                ScaleY = data.ScaleAmount.Y
            });
            group.Children.Add(new TranslateTransform
            {
                X = data.Translate.X,
                Y = data.Translate.Y
            });
            return group;
        }

        public override TransformGroupData ConvertXamlToData(TransformGroup xaml, object parameter = null)
        {
            var translate = xaml.Children[0] as TranslateTransform ?? xaml.Children[1] as TranslateTransform;
            var scale = xaml.Children[0] as ScaleTransform ?? xaml.Children[1] as ScaleTransform;
            return new TransformGroupData(translate == null ? new Point() : new Point(translate.X, translate.Y),
                                          scale == null ? new Point() : new Point(scale.CenterX, scale.CenterY),
                                          scale == null ? new Point(1, 1) : new Point(scale.ScaleX, scale.ScaleY));

        }

        public static TransformGroupDataToGroupTransformConverter Instance;

        static TransformGroupDataToGroupTransformConverter()
        {
            Instance = new TransformGroupDataToGroupTransformConverter();
        }
    }
}
