using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
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
            var documentEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            ImageFieldModel fm = documentEndpoint.GetFieldInDocument(fieldReference) as ImageFieldModel;
            if (fm != null)
            {
                Data = fm.Data;
            }
        }

        /// <summary>
        /// Creates Image using layout information from template and Data 
        /// </summary>
        public override FrameworkElement MakeView(TemplateModel template)
        {
            // cast the template to an image template
            var imageTemplate = template as ImageTemplateModel;
            Debug.Assert(imageTemplate != null);

            // set the image to its source
            var image = new Image();
            image.Source = Data;

            // move the image when its left property changes
            var leftBinding = new Binding
            {
                Source = imageTemplate,
                Path = new PropertyPath("Left"),
                Mode = BindingMode.TwoWay
            };
            image.SetBinding(Canvas.LeftProperty, leftBinding);

            // move the image when its top property changes
            var topBinding = new Binding
            {
                Source = imageTemplate,
                Path = new PropertyPath("Top"),
                Mode = BindingMode.TwoWay
            };
            image.SetBinding(Canvas.TopProperty, topBinding);

            // resize the image when its width property changes
            var widthBinding = new Binding
            {
                Source = imageTemplate,
                Path = new PropertyPath("Width"),
                Mode = BindingMode.TwoWay
            };
            image.SetBinding(Image.WidthProperty, widthBinding);

            // resize the image when its height property changes
            var heightBinding = new Binding
            {
                Source = imageTemplate,
                Path = new PropertyPath("Height"),
                Mode = BindingMode.TwoWay
            };
            image.SetBinding(Image.HeightProperty, heightBinding);

            return image;
        }
    }
}
