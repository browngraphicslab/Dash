using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace Dash
{
    public class ImageToDashUtil : IFileParser
    {

        public async Task<DocumentController> ParseFileAsync(IStorageFile sFile, string uniquePath)
        {
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;  
            //somehow this constant use of .jpg actually works with different file types
            var guid = Guid.NewGuid().ToString() + ".jpg";
            var localFile = await localFolder.CreateFileAsync(guid, CreationCollisionOption.ReplaceExisting);
            await sFile.CopyAndReplaceAsync(localFile);

            var annotatedImage = new AnnotatedImage(new Uri(localFile.Path), null, "",
                Path.GetFileName(sFile.Path) == "" ? "" : Path.GetFileNameWithoutExtension(sFile.Path), 300,
                double.NaN);

            return annotatedImage.Document;
        }
    }
}
