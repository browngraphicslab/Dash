using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using Org.BouncyCastle.Security;

namespace Dash
{
    public class DataVirtualizationSource<T>
    {
        private ObservableCollection<ImageSource> _images;
        private ObservableCollection<UIElement> _visibleElements;
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
            _images = new ObservableCollection<ImageSource>();
            _visibleElements = new ObservableCollection<UIElement>();
            view.PageItemsControl.ItemsSource = _visibleElements;
            view.ScrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            view.SizeChanged += View_SizeChanged;
            view.Loaded += View_Loaded;
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
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
        }

        private void View_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var startIndex = 0;
            var endIndex = 1;
            var scale = e.NewSize.Width / Width;
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

            RenderIndices(startIndex, endIndex);

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

            RenderIndices(startIndex, endIndex);

            _startIndex = startIndex;
            _endIndex = endIndex;
        }

        private void RenderIndices(int startIndex, int endIndex)
        {
            if (!_visibleElements.Any())
            {
                return;
            }

            startIndex = Math.Max(startIndex - bufferSize, 0);
            endIndex = Math.Min(endIndex + bufferSize, _visibleElements.Count);

            var startOffset = Math.Abs(_startIndex - startIndex);
            var startStart = Math.Min(_startIndex, startIndex);
            if (_startIndex > startIndex)
            {
                _view.SelectableElements.InsertRange(0, _view.Strategy.GetSelectableElements(startStart, startStart + startOffset));
            }
            else if (_startIndex < startIndex)
            {
                var removeFromStart = _view.Strategy.GetSelectableElements(startStart, startOffset);
                _view.SelectableElements = _view.SelectableElements.Skip(removeFromStart.Count).ToList();
            }

            var endOffset = Math.Abs(_endIndex - endIndex);
            var endStart = Math.Min(_startIndex, startIndex);
            if (_endIndex < endIndex)
            {
                _view.SelectableElements.AddRange(_view.Strategy.GetSelectableElements(endStart, endStart + endOffset));
            }
            else if (_endIndex > endIndex)
            {
                var removeFromEnd = _view.Strategy.GetSelectableElements(endStart, endOffset);
                _view.SelectableElements = _view.SelectableElements.SkipLast(removeFromEnd.Count).ToList();
            }

            for (var i = startIndex; i < endIndex; i++)
            {
                if (_visibleElements[i] is Image img)
                {
                    if (img.Source != _images[i])
                    {
                        img.Source = _images[i];
                    }
                }
                else
                {
                    _visibleElements[i] = new Image
                    {
                        Source = _images[i],
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                }
            }

            for (var i = 0; i < _images.Count; i++)
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

        public void Add(ImageSource newImage)
        {
            if (!_images.Contains(newImage))
            {
                _images.Add(newImage);
                var i = _images.IndexOf(newImage);
                if (_visibleElements.Count <= i)
                {
                    _visibleElements.Add(new Image
                    {
                        Source = newImage,
                        Margin = new Thickness(0, 0, 0, 10)
                    });
                }
                else if (_visibleElements[i] is Image img)
                {
                    if (img.Source != newImage)
                    {
                        img.Source = newImage;
                    }
                }
            }
        }

        public void Clear()
        {
            _images.Clear();
        }

        public BitmapImage this[int i]
        {
            get
            {
                if (_images.Count <= i)
                {
                    return _images[i] as BitmapImage;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
            set
            {
                if (_images.Count > i)
                {
                    if (!_images[i].Equals(value))
                    {
                        _images[i] = value;
                        if (_visibleElements[i] is Image img)
                        {
                            img.Source = value;
                        }
                    }
                }
                else if (value != null)
                {
                    _images.Add(value);
                    if ((_startIndex != null && _startIndex <= i) && (_endIndex != null && _endIndex >= i))
                    {
                        _visibleElements.Add(new Image {Source = value, Margin = new Thickness(0, 0, 0, 10)});
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
                else
                {
                    throw new InvalidParameterException();
                }
            }
        }
    }
}