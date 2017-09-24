using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Dash
{
    public class PptToDashUtil : IFileParser
    {
        public async Task<DocumentController> ParseAsync(IStorageItem item, string uniquePath)
        {
            var sFile = item as StorageFile;
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync("filename.pptx", CreationCollisionOption.ReplaceExisting);
            await sFile.CopyAndReplaceAsync(file);
            await Windows.System.Launcher.LaunchFileAsync(file);

            throw new NotImplementedException();
        }
    }
}
