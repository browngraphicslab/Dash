using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;

namespace Dash
{
    public class ImageToDashUtil : IFileParser
    {

        public async Task<DocumentController> ParseFileAsync(IStorageFile sFile, string uniquePath)
        {

            // store the file locally
            var localFolder = ApplicationData.Current.LocalFolder;  
            //somehow this constant use of .jpg actually works with different file types
            var guid = Guid.NewGuid() + ".jpg";
            var localFile = await localFolder.CreateFileAsync(guid, CreationCollisionOption.ReplaceExisting);
            await sFile.CopyAndReplaceAsync(localFile);

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
