using Dash.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class ImageTemplateModel : TemplateModel
    {
        public ImageTemplateModel(double left = 0, double top = 0, double width = 0, double height = 0,
            Visibility visibility = Visibility.Visible)
            : base(left, top, width, height, visibility)
        {

        } 
        /// <summary>
        /// Creates Image using layout information from template and Data 
        /// </summary>
        protected override List<UIElement> MakeView(FieldModel fieldModel)
        {
            var imageFieldModel = fieldModel as ImageFieldModel;
            if (imageFieldModel == null && fieldModel is TextFieldModel)
                imageFieldModel = new ImageFieldModel(new Uri((fieldModel as TextFieldModel).Data));
            Debug.Assert(imageFieldModel != null);
            Image image = new Image();
            image.CacheMode = new BitmapCache();
            image.Source = imageFieldModel.Data;
            var txf = new TranslateTransform();
            txf.Y = Top;
            txf.X = Left;
            image.RenderTransform = txf;
            image.Visibility = Visibility;
            image.Width =  Width;
            image.Height = Height;
            return new List<UIElement>(new UIElement[] { image });
        }
    }
}
