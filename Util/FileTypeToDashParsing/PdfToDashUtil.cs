using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using static Dash.NoteDocuments;

namespace Dash
{
    public class PdfToDashUtil : IFileParser
    {
        public async Task<DocumentController> ParseFileAsync(IStorageFile sFile, string uniquePath)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var apath = Path.GetFileName(sFile.Path) == "" ? Guid.NewGuid() + ".pdf" : Path.GetFileName(sFile.Path);
            var localFile = await localFolder.CreateFileAsync(apath, CreationCollisionOption.ReplaceExisting);
            await sFile.CopyAndReplaceAsync(localFile);

            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.DataKey] = new ImageController(new Uri(localFile.Path))
            };
            var dataDoc = new DocumentController(fields, DocumentType.DefaultType);
            return new PdfBox(new DocumentReferenceController(dataDoc.Id, KeyStore.DataKey)).Document;
        }

    }
}
