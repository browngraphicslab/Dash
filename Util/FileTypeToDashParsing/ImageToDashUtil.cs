using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Dash
{
    public class ImageToDashUtil : IFileParser
    {
        public async Task<DocumentController> ParseFileAsync(FileData fileData)
        {
            // store the file locally
            var localFolder = ApplicationData.Current.LocalFolder;
            //somehow this constant use of .jpg actually works with different file types
            var uniqueFilePath = UtilShared.GenerateNewId() + ".jpg";
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


            // create a backing document for the image
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.DataKey] = new ImageController(new Uri(localFile.Path))
            };
            var dataDoc = new DocumentController(fields, DocumentType.DefaultType);

            // return an image box, by setting the height to NaN the image height automatically sizes
            // based on the width according to the aspect ratio
            return new ImageBox(new DocumentReferenceController(dataDoc.Id, KeyStore.DataKey), h: double.NaN).Document;

        }
    }
}
