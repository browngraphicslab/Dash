using System;
using Windows.UI.Xaml.Media.Imaging;

namespace Dash
{
    public class ImageFieldModelController : FieldModelController
    {
        public ImageFieldModelController(Uri data) : base(new ImageFieldModel(data)) { }

        /// <summary>
        ///     The <see cref="ImageFieldModel" /> associated with this <see cref="ImageFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ImageFieldModel ImageFieldModel => FieldModel as ImageFieldModel;

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