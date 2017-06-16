using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    /// <summary>
    /// Field model for holding image data
    /// </summary>
    class ImageFieldModel : FieldModel
    {
        public BitmapImage _data; 

        public BitmapImage Data
        {
            get { return _data; }
            set { SetProperty(ref _data, value); }
        }

        public ImageFieldModel(Uri image)
        {
            Data = new BitmapImage(image);
        }

        protected override void UpdateValue(ReferenceFieldModel fieldReference)
        {
            DocumentController cont = App.Instance.Container.GetRequiredService<DocumentController>();
            ImageFieldModel fm = cont.GetFieldInDocument(fieldReference) as ImageFieldModel;
            if (fm != null)
            {
                Data = fm.Data;
            }
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
