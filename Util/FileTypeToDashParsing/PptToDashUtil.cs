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

        public async Task<DocumentController> ParseFileAsync(IStorageFile sFile, string uniquePath)
        {
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.CreateFileAsync("filename.pptx", CreationCollisionOption.ReplaceExisting);
            await sFile.CopyAndReplaceAsync(file);
            await Windows.System.Launcher.LaunchFileAsync(file);

            return null;
        }
    }
}
