using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Models;

namespace Dash
{
    /// <summary>
    /// Field model for holding image data
    /// </summary>
    class ImageFieldModel : FieldModel
    {
        public BitmapImage Data { get; set; }
        
        public ImageFieldModel(Uri image)
        {
            Data = new BitmapImage(image);
        }

        /// <summary>
        /// Creates Image using layout information from template and Data 
        /// </summary>
        public override UIElement MakeView(TemplateModel template)
        {
            ImageTemplateModel imageTemplate = template as ImageTemplateModel;
            Debug.Assert(imageTemplate != null);
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
