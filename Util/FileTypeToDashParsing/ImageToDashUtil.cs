using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.ApplicationModel.DataTransfer;
using System.IO;
using System.Linq;

namespace Dash
{
    public class ImageToDashUtil : IFileParser
    {

        /// <summary>
        /// Parse a file and save it to the local filesystem
        /// </summary>
        public async Task<DocumentController> ParseFileAsync(StorageFile file)
        {
            var localFile = await CopyFileToLocal(file);

            var title = file.DisplayName;

            return await CreateImageNoteFromLocalFile(localFile, title);
        }


        /// <summary>
        /// Parse a file and save it to the local filesystem
        /// </summary>
        public async Task<DocumentController> ParseFileAsync(FileData fileData,
            DataPackageView dataView=null)
        {
            var localFile = await CopyFileToLocal(fileData, dataView);

            var title = (fileData.File as StorageFile)?.DisplayName ?? fileData.File.Name;

            return await CreateImageNoteFromLocalFile(localFile, title);
        }

        public static async Task<Uri> GetLocalURI(StorageFile file)
        {
            return new Uri((await CopyFileToLocal(file)).Path);
        }

        /// <summary>
        /// Parse a bitmap and save it to the local filesystem
        /// </summary>
        public async Task<DocumentController> ParseBitmapAsync(WriteableBitmap bitmap, string title = null)
        {
            var localFile = await CopyBitmapToLocal(bitmap, title);

            return await CreateImageNoteFromLocalFile(localFile, title);
        }

        /// <summary>
        /// Copy a bitmap to the local file system, returns a refernece to the file in the local filesystem
        /// </summary>
        private async Task<StorageFile> CopyBitmapToLocal(WriteableBitmap bitmap, string title = null)
        {
            var localFile = string.IsNullOrEmpty(title) ? await CreateUniqueLocalFile() :  await CreateLocalFile(title);

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
        private static async Task<StorageFile> CopyFileToLocal(FileData fileData,
            DataPackageView dataView)
        {
            var localFile = await CreateUniqueLocalFile();

            // if the uri filepath is a local file then copy it locally
            if (!fileData.File.FileType.EndsWith(".url"))
            {
                try
                {
                    await fileData.File.CopyAndReplaceAsync(localFile);
                }
                catch (Exception)
                {
                    var bmp = await dataView.GetBitmapAsync();
                    IRandomAccessStreamWithContentType streamWithContent = await bmp.OpenReadAsync();
                    byte[] buffer = new byte[streamWithContent.Size];
                    using (DataReader reader = new DataReader(streamWithContent))
                    {
                        await reader.LoadAsync((uint)streamWithContent.Size);
                        reader.ReadBytes(buffer);
                    }
                    var localFolder = ApplicationData.Current.LocalFolder;
                    var uniqueFilePath = UtilShared.GenerateNewId() + ".jpg"; // somehow this works for all images... who knew
                    localFile.OpenStreamForWriteAsync().Result.Write(buffer, 0, buffer.Count());
                }
            }
            // otherwise stream it from the internet
            else
            {
                await fileData.FileUri.GetHttpStreamToStorageFileAsync(localFile);
            }
            return localFile;
        }

        /// <summary>
        /// Copy a file to the local file system, returns a refernece to the file in the local filesystem
        /// </summary>
        private static async Task<StorageFile> CopyFileToLocal(StorageFile file)
        {
            var localFile = await CreateUniqueLocalFile();

            // if the uri filepath is a local file then copy it locally
            if (!file.FileType.EndsWith(".url"))
            {
                await file.CopyAndReplaceAsync(localFile);
            }
            else
            {
                throw new NotImplementedException();
            }

            return localFile;
        }

        /// <summary>
        /// Create a unique file in the local folder
        /// </summary>
        public static async Task<StorageFile> CreateUniqueLocalFile()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var uniqueFilePath = UtilShared.GenerateNewId() + ".jpg"; // somehow this works for all images... who knew
            var localFile = await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);
            return localFile;
        }

        /// <summary>
        /// Create a unique file in the local folder
        /// </summary>
        public static async Task<StorageFile> CreateLocalFile(string fileName)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var uniqueFilePath = fileName + ".jpg"; // somehow this works for all images... who knew
            var localFile = await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);
            return localFile;
        }

        /// <summary>
        /// Convert a local file which stores an image into an ImageBox, if the title is null the ImageBox doesn't have a Title
        /// </summary>
        public static async Task<DocumentController> CreateImageNoteFromLocalFile(IStorageFile localFile, string title, Point where = new Point())
        {
            Point size = await GetImageSize(localFile);
            double imgWidth = size.X;
            double imgHeight = double.NaN;

            return new ImageNote(new Uri(localFile.Path), where, new Size(imgWidth, imgHeight), title).Document;
        }

        /// <summary>
        /// Return the height and width of an image stored in a randomaccess stream
        /// </summary>
        private static async Task<Point> GetImageSize(IRandomAccessStreamReference streamRef)
        {
           
            const double maxDim = 250;
            double pictureHeight = 0;
            double pictureWidth = 0;
            using (var stream = await streamRef.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);
                pictureHeight = (double) Convert.ToInt32(decoder.OrientedPixelHeight);
                pictureWidth = (double) Convert.ToInt32(decoder.OrientedPixelWidth);

                if (pictureHeight > pictureWidth && pictureHeight > maxDim)
                {
                    pictureWidth = pictureWidth / pictureHeight * maxDim;
                    pictureHeight = maxDim;
                }
                else if (pictureWidth > maxDim)
                {
                    pictureHeight = pictureHeight / pictureWidth * maxDim;
                    pictureWidth = maxDim;
                }
            }
            Point size = new Point(pictureWidth, pictureHeight);
            return size;
        }

       
    }
}
