using System;
using System.Linq;
using Windows.Storage;
using DashShared;
using Windows.UI.Xaml.Media.Imaging;
using DashShared.Models;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds image data
    /// </summary>
    [FieldModelTypeAttribute(TypeInfo.Image)]
    public class ImageModel : FieldModel
    {
        /// <summary>
        /// A <see cref="Uri"/> which points to the <see cref="BitmapImage.UriSource"/>
        /// </summary>
        public Uri Data
        {
            get => localFile == null ? globalUri : new Uri(ApplicationData.Current.LocalFolder.Path + "\\"+ localFile);
            set
            {
                if (value == null)
                {
                    return;
                }

                if (value.IsFile && value.LocalPath.Contains(ApplicationData.Current.LocalFolder.Path))
                {
                    localFile = value.Segments.Last();
                }
                else
                {
                    globalUri = value;
                }
            }
        }

        private string localFile;

        private Uri globalUri;

        public string ByteData = null;

        public ImageModel() : base(null)
        {
            
        }


        /// <summary>
        /// Create a new Image Field Model which represents the image pointed to by the <paramref name="data"/>
        /// </summary>
        /// <param name="data">The uri that the image this field model encapsulates is sourced from</param>
        public ImageModel(Uri path, string bytes = null, string id = null) : base(id)
        {
            Data = path;      
            ByteData = bytes;
        }
    }
}
