using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using System.Linq;
using WPdf = Windows.Data.Pdf;

namespace Dash
{
    public class DataVirtualizationSource
    {
        private const double MaxPageWidth = 1500; // maximum width to render a page.  avoids generating oversized (memory-wise) Bitmaps
        private readonly ObservableCollection<Image> _visibleElements = new ObservableCollection<Image>();
        private readonly List<double> _visibleElementsRenderedWidth = new List<double>();
        private readonly List<double> _visibleElementsTargetedWidth = new List<double>();
        private readonly List<bool>   _visibleElementsIsRendering = new List<bool>();
        private readonly ScrollViewer _scrollViewer;
        private readonly PdfAnnotationView _view;
        private double  _verticalOffset;
        public List<Size> PageSizes;

        public DataVirtualizationSource(PdfAnnotationView view, ScrollViewer scrollviewer, ItemsControl pageItemsControl)
        {
            _view = view;
            _scrollViewer = scrollviewer;
            PageSizes = new List<Size>();
            pageItemsControl.ItemsSource = _visibleElements;
        }
        ~DataVirtualizationSource()
        {
            //Debug.WriteLine("Finalizing DataVirtualizationSource");
        }
        
        public double ScrollViewerContentWidth = 1;

        /// <summary>
        /// Given a vertical offset, return the corresponding 0-index page
        /// </summary>
        public int GetIndex(double verticalOffset)
        {
            var index = 0;
            var scale = _view.ActualWidth / _view.xPdfGrid.ActualWidth;
            var currOffset = verticalOffset - PageSizes[index].Height * scale;
            while (currOffset > 0)
            {
                if (index < PageSizes.Count-1)
                    currOffset -= PageSizes[++index].Height * scale;
                else
                    break;
            }

            return index;
        }
        public void Initialize()
        {
            _visibleElements.Clear();
            for (var i = 0; i < _view.Pages.PageSizes.Count; i++)
            {
                _visibleElements.Add(new Image() { Margin = new Thickness(0, 0, 0, 10), Height=PageSizes[i].Height, Width=PageSizes[i].Width });
                _visibleElementsTargetedWidth.Add(-1);
                _visibleElementsRenderedWidth.Add(-1);
                _visibleElementsIsRendering.Add(false);
            }
            var timer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 0, 0, 100)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                RenderIndices(_scrollViewer.VerticalOffset);
            };
            _scrollViewer.ViewChanging += (s, e) =>
            {
                timer.Stop();
                timer.Start();
            };
            _scrollViewer.SizeChanged   += (s,e) => RenderIndices(_verticalOffset);
            RenderIndices(0);
        }
        private void RenderIndices(double scrollOffset)
        {
            _verticalOffset = scrollOffset;
            if (_scrollViewer.ActualHeight != 0)
            {
                var endIndex   = GetIndex(_scrollViewer.ActualHeight + _verticalOffset) + 1;
                var startIndex = GetIndex(Math.Min(_verticalOffset, _scrollViewer.ExtentHeight - _scrollViewer.ActualHeight)) - 1;
                var pageBuffer = (endIndex - startIndex) / 2;
                startIndex = Math.Max(startIndex - pageBuffer, 0);
                endIndex   = Math.Min(endIndex + pageBuffer, _visibleElements.Count - 1);
                for (var i = 0; i < _visibleElements.Count; i++)
                {
                    var targetWidth = (i < startIndex || i > endIndex) ? 0 : _view.ActualWidth;
                    if (!_visibleElementsIsRendering[i] && targetWidth != _visibleElementsRenderedWidth[i])
                    {
                        _visibleElementsIsRendering[i] = true;
                        _visibleElementsTargetedWidth[i] = targetWidth;
                        RenderPage(i);
                    }
                    else
                    {
                        _visibleElementsTargetedWidth[i] = targetWidth;
                    }
                }
            }
        }
        private async void RenderPage(int pageNum)
        {
            using (var page = _view.PDFdoc?.GetPage((uint)pageNum))
            { 
                while (page != null && _visibleElementsRenderedWidth[pageNum] != _visibleElementsTargetedWidth[pageNum])
                {
                    BitmapSource source = null;
                    var targetWidth = _visibleElementsTargetedWidth[pageNum];
                    if (targetWidth != 0)
                    {
                        Debug.WriteLine("Rendering " + pageNum);
                        var options = new Windows.Data.Pdf.PdfPageRenderOptions();
                        var stream = new InMemoryRandomAccessStream();
                        var screenMap = Util.DeltaTransformFromVisual(new Point(1, 1), _view);
                        var widthRatio = (targetWidth / screenMap.X) / _view.PdfMaxWidth;
                        var box = page.Dimensions.MediaBox;
                        options.DestinationWidth = (uint)Math.Max(600, Math.Min(widthRatio * box.Width, MaxPageWidth));
                        options.DestinationHeight = (uint)Math.Max(600 * box.Height/box.Width, Math.Min(widthRatio * box.Height, MaxPageWidth * box.Height / box.Width));
                        await page.RenderToStreamAsync(stream, options);
                        source = new BitmapImage();
                        await source.SetSourceAsync(stream);
                    }
                    _visibleElements[pageNum].Source = source; 
                    _visibleElementsRenderedWidth[pageNum] = targetWidth;
                }
                _visibleElementsIsRendering[pageNum] = false;
            }
        }

	    public static async Task<WriteableBitmap> GetImageFromPdf(WPdf.PdfDocument pdf, uint pageNum)
	    {
		    WriteableBitmap wb;
		    using (var page = pdf.GetPage(pageNum))
		    {
			    //todo get a way to write out to disk as opposed to memory
			    var stream = new InMemoryRandomAccessStream();
			    await page.RenderToStreamAsync(stream, new WPdf.PdfPageRenderOptions() { DestinationHeight = (uint)page.Size.Height, DestinationWidth = (uint)page.Size.Width });

			    wb = new WriteableBitmap((int)page.Size.Width, (int)page.Size.Height);
			    await wb.SetSourceAsync(stream);
		    }

		    return wb;
	    }

	    public static async Task<Size> RenderPageToFile(WPdf.PdfDocument pdf, uint pageNum, StorageFile file)
	    {
		    using (var page = pdf.GetPage(pageNum))
		    {
			    var randomStream = await file.OpenAsync(FileAccessMode.ReadWrite);
			    await page.RenderToStreamAsync(randomStream, new WPdf.PdfPageRenderOptions { DestinationWidth = (uint)page.Size.Width, BitmapEncoderId = BitmapEncoder.JpegEncoderId });
			    await randomStream.FlushAsync();
			    randomStream.Dispose();
			    return page.Size;
		    }
	    }

	    public static async Task<WPdf.PdfDocument> GetPdf(DocumentController pdf)
	    {
		    var pdfUri = new Uri(pdf.GetDataDocument().GetField<TextController>(KeyStore.SourceUriKey).Data);
		    StorageFile file;
		    try
		    {
			    file = await StorageFile.GetFileFromApplicationUriAsync(pdfUri);
		    }
		    catch (ArgumentException)
		    {
			    try
			    {
				    file = await StorageFile.GetFileFromPathAsync(pdfUri.LocalPath);
			    }
			    catch (ArgumentException)
			    {
				    return null;
			    }
		    }
		    return await WPdf.PdfDocument.LoadFromFileAsync(file);
	    }
	}
}
