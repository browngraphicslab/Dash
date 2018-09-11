using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds pdf data
    /// </summary>
    [FieldModelType(TypeInfo.Pdf)]
    class PdfModel : FieldModel
    {
        private Uri _uriCache;

        /// <summary>
        /// A <see cref="Uri"/> which points to the <see cref="BitmapImage.UriSource"/>
        /// </summary>
        public Uri Data
        {
            get => _uriCache;
            set
            {
                if (value == null)
                {
                    return;
                }

                // if the value is a file and the file exists in the local folder then set localFile to the filename
                if (value.IsFile && File.Exists(ApplicationData.Current.LocalFolder.Path + "\\" + value.Segments.Last()))
                {
                    localFile = value.Segments.Last();
                    globalUri = null;
                }
                else
                {
                    // otherwise assume the file is a globalUri like http so set it there
                    globalUri = value;
                    localFile = null;
                }

                _uriCache = localFile == null ? globalUri : new Uri(ApplicationData.Current.LocalFolder.Path + "\\" + localFile);
            }
        }

        /// <summary>
        /// not null if the file is stored in localfolder
        /// </summary>
        private string localFile;

        /// <summary>
        ///  not null if the file is stored outside localfolder like on the web
        /// </summary>
        private Uri globalUri;

        public string ByteData = null;

        public PdfModel() : base(null)
        {

        }


        /// <summary>
        /// Create a new Pdf Field Model which represents the image pointed to by the <paramref name="data"/>
        /// </summary>
        /// <param name="data">The uri that the image this field model encapsulates is sourced from</param>
        public PdfModel(Uri path, string bytes = null, string id = null) : base(id)
        {
            Data = path;
            ByteData = bytes;
        }

    }
}
