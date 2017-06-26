using System;
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
            }
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
                    // update local
                    // update server
                }
            }
        }
    }


    public class ReferenceFieldModelController : FieldModelController
    {
        /// <summary>
        ///     Create a new <see cref="ReferenceFieldModelController"/> associated with the passed in <see cref="ReferenceFieldModel" />
        /// </summary>
        /// <param name="referenceFieldModel">The model which this controller will be operating over</param>
        public ReferenceFieldModelController(ReferenceFieldModel referenceFieldModel) : base(referenceFieldModel)
        {
            ReferenceFieldModel = referenceFieldModel;
        }

        /// <summary>
        ///     The <see cref="ReferenceFieldModel" /> associated with this <see cref="ReferenceFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ReferenceFieldModel ReferenceFieldModel { get; }
        
    }
}