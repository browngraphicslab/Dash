using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Dash
{
    public class ImageToDashUtil : IFileParser
    {
        /// <summary>
        /// Parse a file and save it to the local filesystem
        /// </summary>
        public async Task<DocumentController> ParseFileAsync(FileData fileData)
        {
            var localFile = await CopyFileToLocal(fileData);

            var title = (fileData.File as StorageFile)?.DisplayName ?? fileData.File.Name;

            return await CreateImageBoxFromLocalFile(localFile, title);
        }

        /// <summary>
        /// Parse a bitmap and save it to the local filesystem
        /// </summary>
        public async Task<DocumentController> ParseBitmapAsync(WriteableBitmap bitmap, string title = null)
        {
            var localFile = await CopyBitmapToLocal(bitmap);

            return await CreateImageBoxFromLocalFile(localFile, title);
        }

        /// <summary>
        /// Copy a bitmap to the local file system, returns a refernece to the file in the local filesystem
        /// </summary>
        private async Task<StorageFile> CopyBitmapToLocal(WriteableBitmap bitmap)
        {
            var localFile = await CreateUniqueLocalFile();

            // open a stream to the local file
            using (var stream = await localFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                var pixelStream = bitmap.PixelBuffer.AsStream();
                var pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                    Convert.ToUInt32(bitmap.PixelWidth), Convert.ToUInt32(bitmap.PixelHeight),
                    96.0,
                    96.0,
                    pixels);
                await encoder.FlushAsync();
            }
            return localFile;
        }

        /// <summary>
        /// Copy a file to the local file system, returns a refernece to the file in the local filesystem
        /// </summary>
        private static async Task<StorageFile> CopyFileToLocal(FileData fileData)
        {
            var localFile = await CreateUniqueLocalFile();

            // if the uri filepath is a local file then copy it locally
            if (!fileData.File.FileType.EndsWith(".url"))
            {
                await fileData.File.CopyAndReplaceAsync(localFile);
            }
            // otherwise stream it from the internet
            else
            {
                await fileData.FileUri.GetHttpStreamToStorageFileAsync(localFile);
            }
            return localFile;
        }

        /// <summary>
        /// Create a unique file in the local folder
        /// </summary>
        private static async Task<StorageFile> CreateUniqueLocalFile()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var uniqueFilePath = UtilShared.GenerateNewId() + ".jpg"; // somehow this works for all images... who knew
            var localFile = await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);
            return localFile;
        }

        /// <summary>
        /// Convert a local file which stores an image into an ImageBox, if the title is null the ImageBox doesn't have a Title
        /// </summary>
        private static async Task<DocumentController> CreateImageBoxFromLocalFile(IStorageFile localFile, string title)
        {
            var imgSize = await GetImageSize(localFile);

            // create a backing document for the image
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.DataKey] = new ImageController(new Uri(localFile.Path)),
                [KeyStore.WidthFieldKey] = new NumberController(imgSize.Width),
                [KeyStore.HeightFieldKey] = new NumberController(imgSize.Height),
            };
            if (title != null) fields[KeyStore.TitleKey] = new TextController(title);
            var dataDoc = new DocumentController(fields, DocumentType.DefaultType);

            // return an image box, by setting the height to NaN the image height automatically sizes
            // based on the width according to the aspect ratio
            return new ImageBox(new DocumentReferenceController(dataDoc.Id, KeyStore.DataKey), h: double.NaN).Document;
        }

        /// <summary>
        /// Return the height and width of an image stored in a randomaccess stream
        /// </summary>
        private static async Task<Size> GetImageSize(IRandomAccessStreamReference streamRef)
        {
            int pictureHeight;
            int pictureWidth;
            using (var stream = await streamRef.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);
                pictureHeight = Convert.ToInt32(decoder.PixelHeight);
                pictureWidth = Convert.ToInt32(decoder.PixelWidth);
            }
            return new Size(pictureWidth, pictureHeight);
        }
    }
}
