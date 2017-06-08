using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Models;

namespace Dash
{
    class ImageFieldModel : FieldModel
    {
        public BitmapImage Data { get; set; }

        public ImageFieldModel(Uri image)
        {
            Data = new BitmapImage(image);
        }

        public override UIElement MakeView(TemplateModel template)
        {
            ImageTemplateModel imageTemplate = template as ImageTemplateModel;
            Image image = new Image();
            image.Source = Data;
            Canvas.SetTop(image, imageTemplate.Top);
            Canvas.SetLeft(image, imageTemplate.Left);
            image.Visibility = imageTemplate.Visibility;
            image.Width = imageTemplate.Width;
            image.Height = imageTemplate.Height;
            return image;
        }
    }
}
