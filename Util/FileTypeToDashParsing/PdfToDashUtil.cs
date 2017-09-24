using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Dash
{
    public class PdfToDashUtil : IFileParser
    {
        public async Task<DocumentController> ParseAsync(IStorageFile item, string uniquePath)
        {
            //var where = sender is CollectionFreeformView ?
            //    Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
            //    new Point();
            //var pdfDoc = new NoteDocuments.CollectionNote(where);
            //var pdf = await PdfDocument.LoadFromFileAsync(item);
            //var children = pdfDoc.DataDocument.GetDereferencedField(NoteDocuments.CollectionNote.CollectedDocsKey, null) as DocumentCollectionFieldModelController;
            //for (uint i = 0; i < pdf.PageCount; i++)
            //    using (var page = pdf.GetPage(i))
            //    {
            //        var src = new BitmapImage();
            //        var stream = new InMemoryRandomAccessStream();
            //        await page.RenderToStreamAsync(stream);
            //        await src.SetSourceAsync(stream);
            //        var pageImage = new Image() { Source = src };

            //        // start of hack to display PDF as a single page image (instead of using a new Pdf document model type)
            //        var renderTargetBitmap = await RenderImportImageToBitmapToOvercomeUWPSandbox(pageImage);
            //        var image = new AnnotatedImage(new Uri(storageFile.Path), await ToBase64(renderTargetBitmap),
            //            300, 300 * renderTargetBitmap.PixelHeight / renderTargetBitmap.PixelWidth, 50, 50);

            //        var pageDoc = new NoteDocuments.CollectionNote(new Point(), Path.GetFileName(storageFile.Path) + ": Page " + i, image.Document).Document;
            //        children?.AddDocument(pageDoc);
            //    }
            //MainPage.Instance.DisplayDocument(pdfDoc.Document, where);

            throw new NotImplementedException();
        }
    }
}
