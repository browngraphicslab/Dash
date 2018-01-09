using System;
using System.IO;

namespace Dash
{
    public class UriToStreamConverter : SafeDataToXamlConverter<Uri, Stream>
    {
        private UriToStreamConverter(){}

        public static UriToStreamConverter Instance;

        static UriToStreamConverter()
        {
            Instance = new UriToStreamConverter();
        }

        public override Stream ConvertDataToXaml(Uri data, object parameter = null)
        {
            var stream = File.Open(data.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return stream;
        }

        /// <summary>
        /// No two way binding since it is impossible to convert a steam to a uri
        /// </summary>
        public override Uri ConvertXamlToData(Stream xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }
}