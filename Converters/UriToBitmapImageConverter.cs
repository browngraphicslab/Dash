using System;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;

namespace Dash
{
    public class UriToBitmapImageConverter : SafeDataToXamlConverter<Uri, BitmapImage>
    {

        private UriToBitmapImageConverter() { }

        public static UriToBitmapImageConverter Instance;

        static UriToBitmapImageConverter()
        {
            Instance = new UriToBitmapImageConverter();
        }

        public override BitmapImage ConvertDataToXaml(Uri data, object parameter = null)
        {
            Debug.Assert(data != null, "You tried to create a new image using a null Uri source");
            return new BitmapImage(data);
        }

        public override Uri ConvertXamlToData(BitmapImage xaml, object parameter = null)
        {
            Debug.Assert(xaml.UriSource != null, "The image you are converting into a Uri does not have a source");
            return xaml.UriSource;
        }
    }
}
