using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

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
        private readonly PdfView _view;
        private double  _verticalOffset;
        public List<Size> PageSizes;

        public DataVirtualizationSource(PdfView view, ScrollViewer scrollviewer, ItemsControl pageItemsControl)
        {
            _view = view;
            _scrollViewer = scrollviewer;
            PageSizes = new List<Size>();
            pageItemsControl.ItemsSource = _visibleElements;
            view.DocumentLoaded += View_Loaded;
        }
        ~DataVirtualizationSource()
        {
            Debug.WriteLine("Finalizing DataVirtualizationSource");
        }

        /// <summary>
        /// Given a vertical offset, return the corresponding 0-index page
        /// </summary>
        public int GetIndex(double verticalOffset)
        {
            var index = 0;
            var scale = _scrollViewer.ActualWidth / _view.PdfMaxWidth;
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
        private void View_Loaded(object sender, EventArgs eventArgs)
        {
            _visibleElements.Clear();
            for (var i = 0; i < _view.PDFdoc?.PageCount; i++)
            {
                _visibleElements.Add(new Image() { Margin = new Thickness(0, 0, 0, 10), Height=PageSizes[i].Height, Width=PageSizes[i].Width });
                _visibleElementsTargetedWidth.Add(-1);
                _visibleElementsRenderedWidth.Add(-1);
                _visibleElementsIsRendering.Add(false);
            }
            
            _scrollViewer.ViewChanging  += (s,e) => RenderIndices(e.FinalView.VerticalOffset);
            _scrollViewer.SizeChanged   += (s,e) => RenderIndices(_verticalOffset);

            _scrollViewer.UpdateLayout(); // bcz: Ugh!  _scrollViewer's ExtentHeight and ActualHeight aren't set when this gets loaded, so we're forced to update it here
            var scrollRatio = _view.LayoutDocument.GetField<NumberController>(KeyStore.PdfVOffsetFieldKey)?.Data ?? 0;
            if (scrollRatio != 0)
            {
                _scrollViewer.ChangeView(null, scrollRatio * _scrollViewer.ExtentHeight, null, true);
            }
            else
                RenderIndices(0);
        }
        private void RenderIndices(double scrollOffset)
        {
            _verticalOffset = scrollOffset;
            if (_scrollViewer.ActualHeight != 0)
            {
                var endIndex   = GetIndex(_scrollViewer.ActualHeight + scrollOffset) + 1;
                var startIndex = GetIndex(Math.Min(scrollOffset, _scrollViewer.ExtentHeight - _scrollViewer.ActualHeight)) - 1;
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
                        if (_view.PdfUri != null)
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
            using (var page = _view.PDFdoc.GetPage((uint)pageNum))
            { 
                while (_visibleElementsRenderedWidth[pageNum] != _visibleElementsTargetedWidth[pageNum])
                {
                    BitmapSource source = null;
                    var targetWidth = _visibleElementsTargetedWidth[pageNum];
                    if (targetWidth != 0)
                    {
                        var options = new Windows.Data.Pdf.PdfPageRenderOptions();
                        var stream = new InMemoryRandomAccessStream();
                        var screenMap = Util.DeltaTransformFromVisual(new Point(1, 1), _view);
                        var widthRatio = (targetWidth / screenMap.X) / _view.PdfMaxWidth;
                        var box = page.Dimensions.MediaBox;
                        options.DestinationWidth = (uint)Math.Min(widthRatio * box.Width, MaxPageWidth);
                        options.DestinationHeight = (uint)Math.Min(widthRatio * box.Height, MaxPageWidth * box.Height / box.Width);
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
    }
}
