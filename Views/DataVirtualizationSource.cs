using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly ObservableCollection<UIElement> _visibleElements;
        private readonly ScrollViewer _scrollViewer;
        private readonly CustomPdfView _view;
        private int _pageBuffer = 1;
        private int _startIndex;
        private int _endIndex;
        private double _verticalOffset;

        public DataVirtualizationSource(CustomPdfView view, ScrollViewer scrollviewer, ItemsControl pageItemsControl)
        {
            _view = view;
            _scrollViewer = scrollviewer;
            _visibleElements = new ObservableCollection<UIElement>();
            PageSizes = new List<Size>();
            pageItemsControl.ItemsSource = _visibleElements;
            view.DocumentLoaded += View_Loaded;
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
                _visibleElements.Add(new Rectangle
                {
                    Width = PageSizes[i].Width,
                    Height = PageSizes[i].Height,
                    Margin = new Thickness(0, 0, 0, 10),
                    Fill = new SolidColorBrush(Colors.White),
                    Tag = false
                });
            }

            // updates the scrollviewer to scroll to the previous scroll position if existent
            var scrollRatio = _view.LayoutDocument.GetField<NumberController>(KeyStore.PdfVOffsetFieldKey);
            if (scrollRatio != null)
            {
                _scrollViewer.UpdateLayout();
                _scrollViewer.ChangeView(null, scrollRatio.Data * _scrollViewer.ExtentHeight, null, true);
            }

            _verticalOffset = scrollRatio?.Data * _scrollViewer.ExtentHeight ?? 0;
            // get the start index and apply the buffer if possible
            var startIndex = GetIndex(_verticalOffset);
            startIndex = Math.Max(startIndex - _pageBuffer, 0);
            _startIndex = startIndex;

            // get the end index and apply the buffer if possible
            var endIndex = GetIndex(_scrollViewer.ViewportHeight + _verticalOffset) + 1;
            endIndex = Math.Min(endIndex + _pageBuffer, _visibleElements.Count - 1);
            _endIndex = endIndex;

            // render the indices requested
            RenderIndices(startIndex, endIndex, true);
            
            _scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
        }

        public void View_SizeChanged()
        {
            if (!_visibleElements.Any()) return;

            // get the start and end indices with buffers
            var startIndex = GetIndex(_verticalOffset);
            var endIndex = GetIndex(_scrollViewer.ViewportHeight + _verticalOffset) + 1;

            // set the page buffer to the amount of pages visible at any given moment
            _pageBuffer = endIndex - startIndex;

            startIndex = Math.Max(startIndex - _pageBuffer, 0);
            endIndex = Math.Min(endIndex + _pageBuffer, _visibleElements.Count - 1);

            // render the requested indices, force them to re-render (since the size has changed)
            RenderIndices(startIndex, endIndex, true);

            _startIndex = startIndex;
            _endIndex = endIndex;
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            // get the start and end vertices with buffers
            _verticalOffset = e.FinalView.VerticalOffset;
            var startIndex = GetIndex(e.FinalView.VerticalOffset);
            var endIndex = GetIndex(_scrollViewer.ViewportHeight + e.FinalView.VerticalOffset) + 1;

            startIndex = Math.Max(startIndex - _pageBuffer, 0);
            endIndex = Math.Min(endIndex + _pageBuffer, _visibleElements.Count - 1);

            // render the requested indices
            RenderIndices(startIndex, endIndex);

            _startIndex = startIndex;
            _endIndex = endIndex;
        }

        public void ForceRender()
        {
            RenderIndices(_startIndex, _endIndex, true);
        }

        private async void RenderIndices(int startIndex, int endIndex, bool forceRender = false)
        {
            // don't re-render anything if we don't need to
            if (startIndex == _startIndex && endIndex == _endIndex && !forceRender)
            {
                return;
            }

            // start with rendering the indices requested
            for (var i = startIndex; i <= endIndex; i++)
            {
                // if the item is curerntly an image and we're forcing a re-render, re-render the image's source
                if (_visibleElements[i] is Image img && forceRender)
                {
                    img.Source = await RenderPage((uint)i);
                }
                // otherwise, if it's currently a rectangle, create a new image with the rendered page
                else if (_visibleElements[i] is Rectangle rect && !(bool)rect.Tag)
                {
                    rect.Tag = true;
                    _visibleElements[i] = new Image
                    {
                        Source = await RenderPage((uint) i),
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                }
                // if it's already an image and we don't want to force a re-render, don't do anything to it
            }

            // unrender anything that's no longer in the range of requested indices
            for (var i = 0; i < _visibleElements.Count; i++)
            {
                if (i < startIndex || i > endIndex)
                {
                    // if it's an image, change it to a rectangle with a matching size
                    if (_visibleElements[i] is Image img)
                    {
                        _visibleElements[i] = new Rectangle
                        {
                            Width = img.ActualWidth,
                            Height = img.ActualHeight,
                            Margin = new Thickness(0, 0, 0, 10),
                            Fill = new SolidColorBrush(Colors.White),
                            Tag = false
                        };
                    }
                }
            }
        }

        private async Task<ImageSource> RenderPage(uint page)
        {
            if (_view.PdfUri == null)
            {
                return null;
            }

            var options = new Windows.Data.Pdf.PdfPageRenderOptions();
            var stream = new InMemoryRandomAccessStream();
            var screenMap = Util.DeltaTransformFromVisual(new Point(1, 1), _view);
            var widthRatio = _view.ActualWidth == 0 ? 1 : (_view.ActualWidth / screenMap.X) / _view.PdfMaxWidth;
            var box = _view.PDFdoc.GetPage(page).Dimensions.MediaBox;
            options.DestinationWidth = (uint)Math.Min(widthRatio * box.Width, 1500);
            options.DestinationHeight = (uint)Math.Min(widthRatio * box.Height, 1500 * box.Height / box.Width);
            await _view.PDFdoc.GetPage(page).RenderToStreamAsync(stream, options);
            var source = new BitmapImage();
            await source.SetSourceAsync(stream);
            return source;
        }
    }
}