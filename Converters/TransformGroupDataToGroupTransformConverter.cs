using System;
using Windows.UI.Xaml.Media;

namespace Dash
{
    class TransformGroupDataToGroupTransformConverter : SafeDataToXamlConverter<TransformGroupData, Transform>
    {
        public override Transform ConvertDataToXaml(TransformGroupData data, object parameter = null)
        {
            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform
            {
                CenterX = 0,
                CenterY = 0,
                ScaleX = data.ScaleAmount.X,
                ScaleY = data.ScaleAmount.Y
            });
            group.Children.Add(new TranslateTransform
            {
                X = data.Translate.X,
                Y = data.Translate.Y
            });
            return new MatrixTransform {Matrix = group.Value};
        }

        public override TransformGroupData ConvertXamlToData(Transform xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
