using DashShared;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;


namespace Dash
{

    /// <summary>
    /// A Field Model which holds video data
    /// </summary>
    [FieldModelTypeAttribute(TypeInfo.Audio)]
    public class AudioModel : FieldModel
    {

        private Uri _uriCache;

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
                }
                else
                {
                    // otherwise assume the file is a globalUri like http so set it there
                    globalUri = value;
                }

                _uriCache = localFile == null ? globalUri : new Uri(ApplicationData.Current.LocalFolder.Path + "\\" + localFile);
            }
        }

        private string localFile;

        private Uri globalUri;

        public AudioModel() : base(null)
        {

        }


        /// <summary>
        /// Create a new Audio Field Model which represents the audio pointed to by the <paramref name="data"/>
        /// </summary>
        /// <param name="data">The uri that the video this field model encapsulates is sourced from</param>
        public AudioModel(Uri path, string id = null) : base(id)
        {
            Data = path;
        }
    }
}

