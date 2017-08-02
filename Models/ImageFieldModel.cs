using System;
using DashShared;
using Windows.UI.Xaml.Media.Imaging;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds image data
    /// </summary>
    public class ImageFieldModel : FieldModel
    {
        /// <summary>
        /// A <see cref="Uri"/> which points to the <see cref="BitmapImage.UriSource"/>
        /// </summary>
        public Uri Data;

        /// <summary>
        /// Create a new Image Field Model that does not represent an image
        /// </summary>
        public ImageFieldModel()//TODO Make this a default image?
        {
        }

        /// <summary>
        /// Create a new Image Field Model which represents the image pointed to by the <paramref name="data"/>
        /// </summary>
        /// <param name="data">The uri that the image this field model encapsulates is sourced from</param>
        public ImageFieldModel(Uri data)
        {
            Data = data;
        }

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            return new FieldModelDTO(TypeInfo.Image, Data);
        }
    }
}
