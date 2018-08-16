using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;

namespace Dash
{
    public class DataVirtualizationSource<T>
    {
        public List<Size> PageSizes;
        private readonly ObservableCollection<Image> _visibleElements = new ObservableCollection<Image>();
        private readonly List<double> _visibleElementsRenderedWidth = new List<double>();
        private readonly List<double> _visibleElementsTargetedWidth = new List<double>();
        private readonly ScrollViewer _scrollViewer;
        private readonly CustomPdfView _view;
        private double  _verticalOffset;

        public DataVirtualizationSource(CustomPdfView view, ScrollViewer scrollviewer, ItemsControl pageItemsControl)
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
        ///     Given a vertical offset, returns an integer that represents in 0-index the page that
        ///     that offset correlates with.
        /// </summary>
        public int GetIndex(double verticalOffset)
        {
            var index = 0;
            var scale = _scrollViewer.ActualWidth / _view.PdfMaxWidth;
            var height = PageSizes[index].Height * scale;
            var currOffset = verticalOffset;
            while (currOffset - height > 0)
            {
                currOffset -= height;
                index++;
                height = index < PageSizes.Count ? PageSizes[index].Height * scale : 10000000000;
            }

            return index;
        }

        private void View_Loaded(object sender, EventArgs eventArgs)
        {
            // initializes the stackpanel with white rectangles
            for (var i = 0; i < _view.PDFdoc?.PageCount; i++)
            {
                _visibleElements.Add(new Image() { Margin = new Thickness(0, 0, 0, 10), Height=PageSizes[i].Height, Width=PageSizes[i].Width });
                _visibleElementsTargetedWidth.Add(-1);
                _visibleElementsRenderedWidth.Add(-1);
            }

            // updates the scrollviewer to scroll to the previous scroll position if existent
            var scrollRatio = _view.LayoutDocument.GetField<NumberController>(KeyStore.PdfVOffsetFieldKey);
            if (scrollRatio != null)
            {
                _scrollViewer.UpdateLayout();
                _scrollViewer.ChangeView(null, scrollRatio.Data * _scrollViewer.ExtentHeight, null, true);
            }
            
              RenderIndices(scrollRatio?.Data * _scrollViewer.ExtentHeight ?? 0);
            
            _scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            _view.SizeChanged += (s, e) => RenderIndices(_verticalOffset);
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            RenderIndices(e.FinalView.VerticalOffset);
        }

        private void RenderIndices(double scrollOffset)
        {
            _verticalOffset = scrollOffset;
            var startIndex = GetIndex(scrollOffset);
            var endIndex = GetIndex(_view.ActualHeight + scrollOffset) + 1;
            var pageBuffer = endIndex - startIndex;
            startIndex = Math.Max(startIndex - pageBuffer, 0);
            endIndex   = Math.Min(endIndex + pageBuffer, _visibleElements.Count - 1);
            for (var i = 0; i < _visibleElements.Count; i++)
            {
                var targetWidth = (i < startIndex || i > endIndex) ? 0 : _view.ActualWidth;
                if (_visibleElementsRenderedWidth[i] < 0 &&
                    targetWidth != _visibleElementsTargetedWidth[i])
                {
                    _visibleElementsRenderedWidth[i] = Math.Abs(_visibleElementsRenderedWidth[i]); // means rendering is in progress
                    _visibleElementsTargetedWidth[i] = targetWidth; // set the target render width
                    RenderPage(i);
                }
                else
                {
                    _visibleElementsTargetedWidth[i] = targetWidth;
                }
            }
        }
        private async void RenderPage(int pageNum)
        {
            if (_view.PdfUri != null)
            {
                using (var page = _view.PDFdoc.GetPage((uint)pageNum))
                {
                    while (Math.Abs(_visibleElementsRenderedWidth[pageNum]) != _visibleElementsTargetedWidth[pageNum] &&
                        (_visibleElementsRenderedWidth[pageNum] != -1 || _visibleElementsTargetedWidth[pageNum] != 0))
                    {
                        var targetWidth = _visibleElementsTargetedWidth[pageNum];
                        if (targetWidth != 0)
                        {
                            var options = new Windows.Data.Pdf.PdfPageRenderOptions();
                            var stream = new InMemoryRandomAccessStream();
                            var screenMap = Util.DeltaTransformFromVisual(new Point(1, 1), _view);
                            var widthRatio = targetWidth == 0 ? 1 : (targetWidth / screenMap.X) / _view.PdfMaxWidth;
                            var box = page.Dimensions.MediaBox;
                            options.DestinationWidth = (uint)Math.Min(widthRatio * box.Width, 1500);
                            options.DestinationHeight = (uint)Math.Min(widthRatio * box.Height, 1500 * box.Height / box.Width);
                            await page.RenderToStreamAsync(stream, options);
                            var source = new BitmapImage();
                            await source.SetSourceAsync(stream);
                            _visibleElements[pageNum].Source = source;
                        }
                        else
                        {
                            _visibleElements[pageNum].Source = null;
                        }
                        _visibleElementsRenderedWidth[pageNum] = targetWidth;
                    }
                    _visibleElementsRenderedWidth[pageNum] = _visibleElementsRenderedWidth[pageNum] == 0 ? -1: - _visibleElementsRenderedWidth[pageNum]; // marks the rendering as complete
                }
            }
        }
    }
}