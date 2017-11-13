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
            var localFile = await localFolder.CreateFileAsync(Path.GetFileName(sFile.Path), CreationCollisionOption.ReplaceExisting);
            await sFile.CopyAndReplaceAsync(localFile);

            return new AnnotatedImage(new Uri(localFile.Path), null, localFile.Name, 300, 300).Document;
        }
    }
}
