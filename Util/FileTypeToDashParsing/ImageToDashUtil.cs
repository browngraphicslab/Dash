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
            var apath = Path.GetFileName(sFile.Path) == "" ? Guid.NewGuid().ToString() + ".jpg" : Path.GetFileName(sFile.Path);
            var localFile = await localFolder.CreateFileAsync(apath, CreationCollisionOption.ReplaceExisting);
            await sFile.CopyAndReplaceAsync(localFile);
            
            return new AnnotatedImage(new Uri(localFile.Path), null, Path.GetFileName(sFile.Path) == "" ? "<Untitled>" : localFile.Path, 300, 300).Document;
        }
    }
}
