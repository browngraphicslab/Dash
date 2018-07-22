using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Org.BouncyCastle.Security;

namespace Dash
{
    public class DataVirtualizationSource<T>
    {
        private ObservableCollection<UIElement> _visibleElements;
        private List<SelectableElement> _selectableElements;
        private List<SelectableElement> _visibleSelectableElements;
        private Dictionary<int, List<SelectableElement>> _selectableElementDictionary;
        private KeyValuePair<int, int> _startEndIndices;
        private ScrollViewer _scrollViewer;
        private int bufferSize = 1;
        private int _startIndex;
        private int _endIndex;
        private CustomPdfView _view;

        public double Width { get; set; }
        public double Height { get; set; }
        private double _verticalOffset;

        public DataVirtualizationSource(CustomPdfView view)
        {
            _view = view;
            _scrollViewer = view.ScrollViewer;
            _selectableElements = new List<SelectableElement>();
            _visibleSelectableElements = new List<SelectableElement>();
            _visibleElements = new ObservableCollection<UIElement>();
            _selectableElementDictionary = new Dictionary<int, List<SelectableElement>>();
            view.PageItemsControl.ItemsSource = _visibleElements;
            view.ScrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            view.Loaded += View_Loaded;
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < _view.PDFdoc?.PageCount; i++)
            {
                _visibleElements.Add(new Rectangle
                {
                    Width = Width,
                    Height = Height,
                    Margin = new Thickness(0, 0, 0, 10)
                });
                _view.PdfTotalHeight += Height + 10;
            }

            var scrollRatio = _view.LayoutDocument.GetField<NumberController>(KeyStore.PdfVOffsetFieldKey);
            if (scrollRatio != null)
            {
                _view.ScrollViewer.UpdateLayout();
                _view.ScrollViewer.ChangeView(null, scrollRatio.Data * _view.ScrollViewer.ExtentHeight, null, true);
            }

            var startIndex = 0;
            var endIndex = 1;
            var scale = _scrollViewer.ViewportWidth / Width;
            var height = Height * scale;
            var temp = _verticalOffset;
            while (temp - height > 0)
            {
                temp -= height;
                startIndex++;
            }

            var endHeight = _scrollViewer.ViewportHeight + _verticalOffset;
            while (endHeight - height > 0)
            {
                endHeight -= height;
                endIndex++;
            }

            _startIndex = startIndex;
            _endIndex = endIndex;

            RenderIndices(startIndex, endIndex, true);
        }

        public void View_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var startIndex = 0;
            var endIndex = 1;
            var scale = e?.NewSize.Width / Width ?? _view.ScrollViewer.ActualWidth / Width;
            var height = Height * scale;
            var temp = _verticalOffset;
            while (temp - height > 0)
            {
                temp -= height;
                startIndex++;
            }

            var endHeight = _scrollViewer.ViewportHeight + _verticalOffset;
            while (endHeight - height > 0)
            {
                endHeight -= height;
                endIndex++;
            }

            startIndex = Math.Max(startIndex - bufferSize, 0);
            endIndex = Math.Min(endIndex + bufferSize, _visibleElements.Count);

            RenderIndices(_startIndex, _endIndex, true);

            _startIndex = startIndex;
            _endIndex = endIndex;
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            _verticalOffset = e.FinalView.VerticalOffset;
            var startIndex = 0;
            var endIndex = 1;
            var scale = _scrollViewer.ViewportWidth / Width;
            var height = Height * scale;
            var temp = e.FinalView.VerticalOffset;
            while (temp - height > 0)
            {
                temp -= height;
                startIndex++;
            }

            var endHeight = _scrollViewer.ViewportHeight + e.FinalView.VerticalOffset;
            while (endHeight - height > 0)
            {
                endHeight -= height;
                endIndex++;
            }

            startIndex = Math.Max(startIndex - bufferSize, 0);
            endIndex = Math.Min(endIndex + bufferSize, _visibleElements.Count);

            RenderIndices(startIndex, endIndex);

            _startIndex = startIndex;
            _endIndex = endIndex;
        }

        private async void RenderIndices(int startIndex, int endIndex, bool forceRender = false)
        {
            if (!_visibleElements.Any())
            {
                for (var i = 0; i < _view.PDFdoc?.PageCount; i++)
                {
                    if (startIndex <= i && i <= endIndex)
                    {
                        _visibleElements.Add(new Image
                        {
                            Source = await RenderPage((uint) i),
                            Margin = new Thickness(0, 0, 0, 10)
                        });
                    }
                    else
                    {
                        _visibleElements.Add(new Rectangle
                        {
                            Width = Width,
                            Height = Height,
                            Margin = new Thickness(0, 0, 0, 10)
                        });
                    }
                }
                return;
            }

            if (startIndex == _startIndex && endIndex == _endIndex && !forceRender)
            {
                return;
            }

            //_startEndIndices = new KeyValuePair<int, int>(startIndex, endIndex);
            //var startOffset = Math.Abs(_startIndex - startIndex);
            //var startStart = Math.Min(_startIndex, startIndex);
            //if (_startIndex > startIndex)
            //{
            //    _view.SelectableElements.InsertRange(0, _view.Strategy.GetSelectableElements(startStart, _startIndex));
            //}
            //else if (_startIndex < startIndex)
            //{
            //    var removeFromStart = _view.Strategy.GetSelectableElements(startStart, startOffset);
            //    _view.SelectableElements = _view.SelectableElements.Skip(removeFromStart.Count).ToList();
            //}

            //var endOffset = Math.Abs(_endIndex - endIndex);
            //var endStart = Math.Min(_startIndex, startIndex);
            //if (_endIndex < endIndex)
            //{
            //    _view.SelectableElements.AddRange(_view.Strategy.GetSelectableElements(endStart, _endIndex));
            //}
            //else if (_endIndex > endIndex)
            //{
            //    var removeFromEnd = _view.Strategy.GetSelectableElements(endStart, endOffset);
            //    _view.SelectableElements = _view.SelectableElements.SkipLast(removeFromEnd.Count).ToList();
            //}

            var elements = new List<SelectableElement>();
            for (var i = startIndex; i < endIndex; i++)
            {
                if (_visibleElements[i] is Image img && forceRender)
                {
                    Debug.WriteLine($"Page {i} is being loaded");
                    img.Source = await RenderPage((uint)i);
                }
                else
                {
                    Debug.WriteLine($"Page {i} is being loaded");
                    _visibleElements[i] = new Image
                    {
                        Source = await RenderPage((uint) i),
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                }

                //elements.AddRange(_selectableElementDictionary[i]);
            }

            //_view.SelectableElements = elements;

            for (var i = 0; i < _visibleElements.Count; i++)
            {
                if (i < startIndex || i > endIndex)
                {
                    if (_visibleElements[i] is Image img)
                    {
                        _visibleElements[i] = new Rectangle
                        {
                            Width = img.ActualWidth,
                            Height = img.ActualHeight,
                            Margin = new Thickness(0, 0, 0, 10)
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

            StorageFile file;
            try
            {
                file = await StorageFile.GetFileFromPathAsync(_view.PdfUri.LocalPath);
            }
            catch (ArgumentException)
            {
                try
                {
                    file = await StorageFile.GetFileFromApplicationUriAsync(_view.PdfUri);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            var reader = new PdfReader(await file.OpenStreamForReadAsync());
            var pdfDocument = new PdfDocument(reader);
            var strategy = new BoundsExtractionStrategy();
            var processor = new PdfCanvasProcessor(strategy);

            var options = new Windows.Data.Pdf.PdfPageRenderOptions();
            var stream = new InMemoryRandomAccessStream();
            var widthRatio = _view.ActualWidth == 0 ? 1 : _view.ActualWidth / _view.PdfMaxWidth;
            options.DestinationWidth = (uint)(widthRatio * _view.PDFdoc.GetPage(page).Dimensions.MediaBox.Width);
            options.DestinationHeight = (uint)(widthRatio * _view.PDFdoc.GetPage(page).Dimensions.MediaBox.Height);
            await _view.PDFdoc.GetPage(page).RenderToStreamAsync(stream, options);
            var source = new BitmapImage();
            await source.SetSourceAsync(stream);

            if (_selectableElementDictionary.ContainsKey((int) page))
            {
                _selectableElementDictionary[(int) page] = strategy.GetSelectableElements((int) page, (int) page);
            }
            else
            {
                _selectableElementDictionary.Add((int) page, strategy.GetSelectableElements((int) page, (int) page));
            }
            return source;
        }

        public void Clear()
        {

        }
    }
}