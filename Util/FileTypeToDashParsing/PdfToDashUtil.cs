﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static Dash.NoteDocuments;

namespace Dash
{
    public class PdfToDashUtil : IFileParser
    {
        public async Task<DocumentController> ParseFileAsync(IStorageFile storageFile, string uniquePath=null)
        {
            //var where = sender is Covar storageFile = items[0] as StorageFile;
            if (storageFile.Path.EndsWith(".pdf"))
            {
                var pdfDoc = new CollectionNote(new Point(), CollectionView.CollectionViewType.Page);
                var pdf = await PdfDocument.LoadFromFileAsync(storageFile);
                var children = pdfDoc.DataDocument.GetDereferencedField(CollectionNote.CollectedDocsKey, null) as DocumentCollectionFieldModelController;
                for (uint i = 0; i < pdf.PageCount; i++)
                    using (var page = pdf.GetPage(i))
                    {
                        var src = new BitmapImage();
                        var stream = new InMemoryRandomAccessStream();
                        await page.RenderToStreamAsync(stream);
                        await src.SetSourceAsync(stream);
                        var pageImage = new Image() { Source = src };

                        // start of hack to display PDF as a single page image (instead of using a new Pdf document model type)
                        var renderTargetBitmap = await RenderImportImageToBitmapToOvercomeUWPSandbox(pageImage);
                        var image = new AnnotatedImage(new Uri(storageFile.Path), await ToBase64(renderTargetBitmap),
                            300, 300 * renderTargetBitmap.PixelHeight / renderTargetBitmap.PixelWidth, 50, 50);

                        var pageDoc = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform, Path.GetFileName(storageFile.Path) + ": Page " + i, image.Document).Document;
                        children?.AddDocument(pageDoc);
                    }
                return pdfDoc.Document;
            }
            return null;
        }
        async Task<string> ToBase64(RenderTargetBitmap bitmap)
        {
            var image = (await bitmap.GetPixelsAsync()).ToArray();
            var width = (uint)bitmap.PixelWidth;
            var height = (uint)bitmap.PixelHeight;

            double dpiX = 96;
            double dpiY = 96;

            var encoded = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, encoded);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, width, height, dpiX, dpiY, image);
            await encoder.FlushAsync();
            encoded.Seek(0);

            var bytes = new byte[encoded.Size];
            await encoded.AsStream().ReadAsync(bytes, 0, bytes.Length);

            var base64String = Convert.ToBase64String(bytes);
            return base64String;
        }

        public static async Task<RenderTargetBitmap> RenderImportImageToBitmapToOvercomeUWPSandbox(Image imagery)
        {
            Grid HackGridToRenderImage, HackGridToHideRenderImageWhenRendering;

            HackGridToRenderImage = new Grid();
            HackGridToHideRenderImageWhenRendering = new Grid();
            var w = (imagery.Source as BitmapImage).PixelWidth;
            var h = (imagery.Source as BitmapImage).PixelHeight;
            if (w == 0)
                w = 100;
            if (h == 0)
                h = 100;
            imagery.Width = HackGridToRenderImage.Width = HackGridToHideRenderImageWhenRendering.Width = w;
            imagery.Height = HackGridToRenderImage.Height = HackGridToHideRenderImageWhenRendering.Height = h;
            //HackGridToHideRenderImageWhenRendering.Background = new SolidColorBrush(Colors.Blue);
            HackGridToHideRenderImageWhenRendering.Children.Add(HackGridToRenderImage);
            HackGridToRenderImage.Background = new SolidColorBrush(Colors.Blue);
            HackGridToRenderImage.Children.Add(imagery);
            HackGridToHideRenderImageWhenRendering.Opacity = 0.0;

            var renderTargetBitmap = new RenderTargetBitmap();
            (MainPage.Instance.xMainDocView.Content as Grid).Children.Add(HackGridToHideRenderImageWhenRendering);
            await renderTargetBitmap.RenderAsync(HackGridToRenderImage);
            (MainPage.Instance.xMainDocView.Content as Grid).Children.Remove(HackGridToHideRenderImageWhenRendering);

            return renderTargetBitmap;
        }

    }
}
