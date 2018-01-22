using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Dash
{
    public class PdfToDashUtil : IFileParser
    {

        public async Task<DocumentController> ParseFileAsync(FileData fileData)
        {
            var localFile = await CopyFileToLocal(fileData);
            var pdfText = await GetPdfText(localFile);
            var title = (fileData.File as StorageFile)?.DisplayName ?? fileData.File.Name;


            // create a backing document for the pdf
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.DataKey] = new ImageController(new Uri(localFile.Path)),
                [KeyStore.DocumentTextKey] = new TextController(pdfText),
                [KeyStore.TitleKey] = new TextController(title)
            };
            var dataDoc = new DocumentController(fields, DocumentType.DefaultType);

            // return a new pdf box
            return new PdfBox(new DocumentReferenceController(dataDoc.Id, KeyStore.DataKey)).Document;
        }

        private async Task<string> GetPdfText(IStorageFile localFile)
        {
            var outputText = string.Empty;

            var pdf = await PdfDocument.LoadFromFileAsync(localFile);
            for (uint pageIndex = 0; pageIndex < pdf.PageCount; pageIndex++)
            {
                using (var stream = new InMemoryRandomAccessStream())
                {
                    await pdf.GetPage(pageIndex).RenderToStreamAsync(stream);
                    var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    Debug.Assert(ocrEngine != null, "ocrEngine should never be null but if it is we need to cleanly fail");
                    var result = await ocrEngine.RecognizeAsync(await decoder.GetSoftwareBitmapAsync());
                    outputText += $"{result.Text}\n ";
                }
            }

            return outputText;
        }

        private static async Task<StorageFile> CopyFileToLocal(FileData fileData)
        {
            // store the file locally
            var localFolder = ApplicationData.Current.LocalFolder;
            var uniqueFilePath = Guid.NewGuid() + ".pdf";
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
            return localFile;
        }
    }
}
