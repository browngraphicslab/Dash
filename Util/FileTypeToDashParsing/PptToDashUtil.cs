using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Microsoft.Toolkit.Uwp;

namespace Dash
{
    public class PptToDashUtil : IFileParser
    {

        public async Task<DocumentController> ParseFileAsync(FileData fileData)
        {
            // store the file locally
            var localFolder = ApplicationData.Current.LocalFolder;
            var uniqueFilePath = UtilShared.GenerateNewId() + ".pptx";
            var localFile = await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);

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

            await Windows.System.Launcher.LaunchFileAsync(localFile);

            return null;
        }
    }
}
