using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class ImageTemplateModel : TemplateModel
    {
        private bool fill;
        public ImageTemplateModel(double left = 0, double top = 0, double width = 0, double height = 0,
            Visibility visibility = Visibility.Visible, bool fill = false)
            : base(left, top, width, height, visibility)
        {
            this.fill = fill;
        }
        
        /// <summary>
        /// Creates Image using layout information from template and Data 
        /// </summary>
        public override UIElement MakeView(FieldModel fieldModel)
        {
            ImageFieldModel imageFieldModel = fieldModel is TextFieldModel ? new ImageFieldModel(new Uri((fieldModel as TextFieldModel).Data)) :  fieldModel as ImageFieldModel;
            Debug.Assert(imageFieldModel != null);
            Image image = new Image();
            image.Source = imageFieldModel.Data;
            Canvas.SetTop(image, Top);
            Canvas.SetLeft(image, Left);
            image.Visibility = Visibility;
            image.Width = Width;
            image.Height = Height;
            if (fill)
                image.Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill;
            return image;
        }
    }
}
