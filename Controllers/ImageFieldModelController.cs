using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using DashShared.Models;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media;
using Windows.Storage;
using System.Diagnostics;

namespace Dash
{
    public class ImageFieldModelController : FieldModelController<ImageFieldModel>
    {

        public ImageFieldModelController() : base(new ImageFieldModel()) { }

        public ImageFieldModelController(Uri path, string data = null) : base(new ImageFieldModel(path, data)) { }

        public ImageFieldModelController(ImageFieldModel imageFieldModel) : base(imageFieldModel)
        {

        }

        public override void Init()
        {

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
            if (value is BitmapImage)
            {
                Data = value as BitmapImage;
                return true;
            }
            return false;
        }

        public BitmapImage ByteToImage(byte[] array)
        {
            BitmapImage image = new BitmapImage();
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                stream.AsStreamForWrite().Write(array, 0, array.Length);
                stream.Seek(0);
                image.SetSource(stream);
            }
            return image;
        }

        ImageSource _cacheSource;
        /// <summary>
        ///     The image which this image controller is attached to. This is the <see cref="BitmapImage" /> representation of
        ///     the <see cref="ImageFieldModel.Data" />
        /// </summary>
        public ImageSource Data
        {
            get {
                if (_cacheSource == null) {
                    if (ImageFieldModel.Data != null)
                    {
                        _cacheSource = UriToBitmapImageConverter.Instance.ConvertDataToXaml(ImageFieldModel.Data);
                    }
                    if (ImageFieldModel.ByteData != null)
                    {
                        _cacheSource = FromBase64(ImageFieldModel.ByteData);
                    }
                }
                return _cacheSource;
            }   //TODO We shouldn't create a new BitmapImage every time Data is accessed
            set {
                _cacheSource = null;

                if (value is BitmapImage)
                {
                    ImageFieldModel.Data = UriToBitmapImageConverter.Instance.ConvertXamlToData(value as BitmapImage);
                    OnFieldModelUpdated(null);
                }
            }
        }
        private WriteableBitmap FromBase64(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            var image = bytes.AsBuffer().AsStream().AsRandomAccessStream();

            BitmapDecoder decoder = null;

            var x = Task.Run(async () => {
                decoder = await BitmapDecoder.CreateAsync(image).AsTask();
                return 0;
            }).Result;
            image.Seek(0);

            var bmp = new WriteableBitmap((int)decoder.PixelHeight, (int)decoder.PixelWidth);
            MainPage.Instance.Dispatcher.RunIdleAsync(async (args) =>
            {
                bmp.SetSourceAsync(image);
            });
            return bmp;
        }
        public override TypeInfo TypeInfo => TypeInfo.Image;

        public override string ToString()
        {
            return ImageFieldModel.Data.AbsolutePath;
        }

        public override FieldModelController<ImageFieldModel> Copy()
        {
            return new ImageFieldModelController(ImageFieldModel.Data, ImageFieldModel.ByteData);
        }
    }
}