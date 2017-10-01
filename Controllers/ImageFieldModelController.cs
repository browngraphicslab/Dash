using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class ImageFieldModelController : FieldModelController<ImageFieldModel>
    {
        public ImageFieldModelController(Uri data) : base(new ImageFieldModel(data)) { }

        private ImageFieldModelController(ImageFieldModel imageFieldModel) : base(imageFieldModel)
        {

        }

        public static ImageFieldModelController CreateFromServer(ImageFieldModel imageFieldModel)
        {
            return ContentController<FieldModel>.GetController<ImageFieldModelController>(imageFieldModel.Id) ??
                   new ImageFieldModelController(imageFieldModel);
        }

        /// <summary>
        ///     The <see cref="ImageFieldModel" /> associated with this <see cref="ImageFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ImageFieldModel ImageFieldModel => Model as ImageFieldModel;

        /// <summary>
        ///     The uri which this image is sourced from. This is a wrapper for <see cref="ImageFieldModel.Data" />
        /// </summary>
        public Uri ImageSource
        {
            get { return ImageFieldModel.Data; }
            set
            {
                if (ImageFieldModel.Data != value)
                {
                    ImageFieldModel.Data = value;
                    // Update the server
                    UpdateOnServer();
                    OnFieldModelUpdated(null);
                    // update local
                    // update server    
                }
            }
        }

        public override FrameworkElement GetTableCellView(Context context)
        {
            var image = new Image
            {
                Source = new BitmapImage(ImageSource),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
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

        public override FieldControllerBase GetDefaultController()
        {
            return new ImageFieldModelController(new Uri("ms-appx:///Assets/DefaultImage.png"));
        }

        public override object GetValue(Context context)
        {
            return Data;
        }

        public override bool SetValue(object value)
        {
            if (value is string)
            {
                Data.UriSource = new Uri(value as string);
                return true;
            }
            else if (value is BitmapImage)
            {
                Data = value as BitmapImage;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     The image which this image controller is attached to. This is the <see cref="BitmapImage" /> representation of
        ///     the <see cref="ImageFieldModel.Data" />
        /// </summary>
        public BitmapImage Data
        {
            get { return UriToBitmapImageConverter.Instance.ConvertDataToXaml(ImageFieldModel.Data); }//TODO We shouldn't create a new BitmapImage every time Data is accessed
            set
            {
                if (value.UriSource != ImageFieldModel.Data)
                {
                    ImageFieldModel.Data = UriToBitmapImageConverter.Instance.ConvertXamlToData(value);
                    OnFieldModelUpdated(null);
                }
            }
        }
        public override TypeInfo TypeInfo => TypeInfo.Image;

        public override string ToString()
        {
            return ImageSource.ToString();
        }

        public override FieldModelController<ImageFieldModel> Copy()
        {
            return new ImageFieldModelController(Data.UriSource);
        }
    }
}