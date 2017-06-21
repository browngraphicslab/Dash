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
        public ImageFieldModel(Image image) {
            Data = (BitmapImage)image.Source;
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
    }
}
