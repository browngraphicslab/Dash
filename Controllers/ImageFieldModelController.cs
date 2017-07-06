﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Dash
{
    public class ImageFieldModelController : FieldModelController
    {
        /// <summary>
        ///     Create a new <see cref="ImageFieldModelController"/> associated with the passed in <see cref="ImageFieldModel" />
        /// </summary>
        /// <param name="imageFieldModel">The model which this controller will be operating over</param>
        public ImageFieldModelController(ImageFieldModel imageFieldModel) : base(imageFieldModel)
        {
            ImageFieldModel = imageFieldModel;
        }

        /// <summary>
        ///     The <see cref="ImageFieldModel" /> associated with this <see cref="ImageFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ImageFieldModel ImageFieldModel { get; }

        /// <summary>
        ///     The uri which this image is sourced from. This is a wrapper for <see cref="ImageFieldModel.Data" />
        /// </summary>
        public Uri ImageSource
        {
            get { return ImageFieldModel.Data; }
            set
            {
                if (SetProperty(ref ImageFieldModel.Data, value))
                {
                    // update local
                    // update server    
                }
                FireFieldModelUpdated();
            }
        }

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            Data = (fieldModel as ImageFieldModelController).Data;
        }

        public override FrameworkElement GetTableCellView()
        {
            var image = new Image
            {
                Source = new BitmapImage(ImageSource),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            var imageSourceBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Data)),
                Mode = BindingMode.OneWay
            };
            image.SetBinding(Image.SourceProperty, imageSourceBinding);

            return image;
        }

        /// <summary>
        ///     The image which this image controller is attached to. This is the <see cref="BitmapImage" /> representation of
        ///     the <see cref="ImageFieldModel.Data" />
        /// </summary>
        public BitmapImage Data
        {
            get { return UriToBitmapImageConverter.Instance.ConvertDataToXaml(ImageFieldModel.Data); }
            set
            {
                if (SetProperty(ref ImageFieldModel.Data, UriToBitmapImageConverter.Instance.ConvertXamlToData(value)))
                {
                    FireFieldModelUpdated();
                    // update local
                    // update server
                }
            }
        }

        public override string ToString()
        {
            return ImageSource.ToString();
        }
    }
}