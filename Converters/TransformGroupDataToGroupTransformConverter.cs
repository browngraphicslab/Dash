using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            return group;
        }

        public override TransformGroupData ConvertXamlToData(TransformGroup xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}
