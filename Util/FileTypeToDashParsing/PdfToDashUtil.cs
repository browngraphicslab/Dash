using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using DashShared;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.ApplicationModel.DataTransfer;
using MyToolkit.Resources;
using Dash.Controllers;

namespace Dash
{

    public class PdfToDashUtil : IFileParser
    {
	    public static DocumentType PdfType = new DocumentType("150FD51D-E421-44ED-A3C8-AB9A932B2C7C", "Pdf Note");

        public async Task<DocumentController> ParseFileAsync(FileData fileData, DataPackageView dataView = null)
        {
            var localFile = await CopyFileToLocal(fileData);
            return GetPDFDoc(localFile, fileData.File.Name);
        }

        //TODO This should be temporary for the chrome extension
        public async Task<DocumentController> UriToDoc(Uri uri)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var uniqueFilePath = Guid.NewGuid() + ".pdf";
            var localFile = await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);
            await uri.GetHttpStreamToStorageFileAsync(localFile);
            return GetPDFDoc(localFile, uri.LocalPath.Trim('/'));
        }

        public DocumentController GetPDFDoc(StorageFile file, string title = null)
        {
            title = title ?? file.DisplayName;

            // create a backing document for the pdf
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.DataKey] = new PdfController(new Uri(file.Path)),
                [KeyStore.TitleKey] = new TextController(title),
                [KeyStore.DateCreatedKey] = new DateTimeController(),
                [KeyStore.AuthorKey] = new TextController(MainPage.Instance.GetSettingsView.UserName)
            };
            var dataDoc = new DocumentController(fields, PdfType);

//#pragma warning disable 4014
//            Task.Run(async () =>
//            {
//                var text = await GetPdfText(file);
//                UITask.Run(() => dataDoc.SetField(KeyStore.DocumentTextKey, new TextController(text), true));
//            });

            // return a new pdf box
            var layout =  new PdfBox(new DocumentReferenceController(dataDoc, KeyStore.DataKey)).Document;
            layout.SetField(KeyStore.DocumentContextKey, dataDoc, true);
            var docContextReference = new DocumentReferenceController(layout, KeyStore.DocumentContextKey);
            layout.SetField(KeyStore.TitleKey, new PointerReferenceController(docContextReference, KeyStore.TitleKey), true);
            return layout;
        }

        private async Task<string> GetPdfText(IStorageFile localFile)
        {
            var outputText = string.Empty;
            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            var pdf = await PdfDocument.LoadFromFileAsync(localFile);
            for (uint pageIndex = 0; pageIndex < pdf.PageCount; pageIndex++)
            {
                using (var stream = new InMemoryRandomAccessStream())
                {
                    await pdf.GetPage(pageIndex).RenderToStreamAsync(stream);
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    Debug.Assert(ocrEngine != null, "ocrEngine should never be null but if it is we need to cleanly fail");
                    var result = await ocrEngine.RecognizeAsync(await decoder.GetSoftwareBitmapAsync());
                    outputText += $"{result.Text}\n";
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
