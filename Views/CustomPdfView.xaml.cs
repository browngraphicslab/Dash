﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Point = Windows.Foundation.Point;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using WPdf = Windows.Data.Pdf;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CustomPdfView : UserControl, INotifyPropertyChanged, IVisualAnnotatable
    {
        public static readonly DependencyProperty PdfUriProperty = DependencyProperty.Register(
            "PdfUri", typeof(Uri), typeof(CustomPdfView), new PropertyMetadata(default(Uri), PropertyChangedCallback));

        public Uri PdfUri
        {
            get { return (Uri)GetValue(PdfUriProperty); }
            set { SetValue(PdfUriProperty, value); }
        }

        public double PageSpacing
        {
            get => _pageSpacing;
            private set
            {
                _pageSpacing = value;
                OnPropertyChanged();
            }
        }

        private double _pdfPageSpacing = 20;

        public event EventHandler DocumentLoaded;

        private List<Image> _pages;
        public List<Image> Pages
        {
            get => _pages;
            set
            {
                _pages = value;
                OnPropertyChanged();
            }
        }

        private List<BoundsExtractionStrategy.SelectableElement> _selectableElements;

        private VisualAnnotationManager _annotationManager;
        private double _maxPdfPageWidth = 1;

        private double _scrollRatio; // scroll bar position as a ratio of its location to the size of the scrollable document
        public DocumentController LayoutDocument { get; }
        public DocumentController DataDocument { get; }

        private PdfDocument _pdfDocument;

        private WPdf.PdfDocument _wPdfDocument;

        public CustomPdfView()
        {
            this.InitializeComponent();
        }

        public CustomPdfView(DocumentController document)
        {
            this.InitializeComponent();
            LayoutDocument = document.GetActiveLayout() ?? document;
            DataDocument = document.GetDataDocument();
            _annotationManager = new VisualAnnotationManager(this, LayoutDocument, xAnnotations);
        }

        public DocumentController GetRegionDocument()
        {
            return _annotationManager?.GetRegionDocument();
        }

        private async Task OnPdfUriChanged()
        {
            if (PdfUri == null)
            {
                return;
            }

            StorageFile file;
            try
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(PdfUri);
            }
            catch (ArgumentException)
            {
                try
                {
                    file = await StorageFile.GetFileFromPathAsync(PdfUri.LocalPath);
                }
                catch (ArgumentException)
                {
                    return;
                }
            }
            _wPdfDocument = await WPdf.PdfDocument.LoadFromFileAsync(file);
            await RenderPdf(null);
            DocumentLoaded?.Invoke(this, new EventArgs());

            PdfReader reader = new PdfReader(await file.OpenStreamForReadAsync());
            _pdfDocument = new PdfDocument(reader);
            var strategy = new BoundsExtractionStrategy();
            var processor = new PdfCanvasProcessor(strategy);
            double offset = 0;
            for (int i = 1; i <= _pdfDocument.GetNumberOfPages(); ++i)
            {
                var page = _pdfDocument.GetPage(i);
                var size = page.GetPageSize();
                _maxPdfPageWidth = Math.Max(_maxPdfPageWidth, size.GetWidth());
                strategy.SetPage(i - 1, offset, size);
                offset += page.GetPageSize().GetHeight() + _pdfPageSpacing;
                processor.ProcessPageContent(page);
            }

            TestSelectionCanvas.Width = _maxPdfPageWidth;
            TestSelectionCanvas.Height = offset - _pdfPageSpacing;

            _selectableElements = strategy.GetSelectableElements();
        }

        private async Task RenderPdf(double ?targetWidth)
        {
            List<Image> pages = new List<Image>((int)_wPdfDocument.PageCount);
            var options = new WPdf.PdfPageRenderOptions();
            for (uint i = 0; i < _wPdfDocument.PageCount; ++i)
            {
                var stream = new InMemoryRandomAccessStream();
                if (targetWidth != null)
                {
                    options.DestinationWidth  = (uint) (targetWidth * _wPdfDocument.GetPage(i).Dimensions.MediaBox.Width / _maxPdfPageWidth);
                    options.DestinationHeight = (uint) (options.DestinationWidth *
                    _wPdfDocument.GetPage(i).Dimensions.MediaBox.Height /
                        _wPdfDocument.GetPage(i).Dimensions.MediaBox.Width );
                }
                await _wPdfDocument.GetPage(i).RenderToStreamAsync(stream, options);
                var source = new BitmapImage();
                await source.SetSourceAsync(stream);
                Image page = new Image() { Source = source };
                pages.Add(page);
            }

            Pages = pages;
        }

        private static async void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            await ((CustomPdfView)dependencyObject).OnPdfUriChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RegionSelected(object region, Point pt, DocumentController chosenDoc = null)
        {
            _annotationManager?.RegionSelected(region, pt, chosenDoc);
        }

        public FrameworkElement Self()
        {
            return this;
        }

        public Size GetTotalDocumentSize()
        {
            return new Size(PageItemsControl.ActualWidth, PageItemsControl.ActualHeight);
        }

        public FrameworkElement GetPositionReference()
        {
            return PageItemsControl;
        }

        public DocumentController GetDocControllerFromSelectedRegion()
        {
            var dc = new RichTextNote("PDF " + ScrollViewer.VerticalOffset).Document;
            dc.SetRegionDefinition(LayoutDocument);

            return dc;
        }

	    public VisualAnnotationManager GetAnnotationManager()
	    {
		    return _annotationManager;
	    }

	    private void ItemsControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            NewRegionEnded?.Invoke(sender, e);
            //if (_selectionStart == null)
            //{
            //    return;
            //}
            //var pos = e.GetCurrentPoint(PageItemsControl).Position;
            //var ratio = _maxPdfPageWidth / PageItemsControl.ActualWidth;
            //pos.X = pos.X * ratio;
            //pos.Y = pos.Y * ratio;
            //foreach (var selectableElement in _selectableElements)
            //{
            //    if (selectableElement.Bounds.Contains(pos))
            //    {
            //        int startIndex = Math.Min(selectableElement.Index, _selectionStart.Index);
            //        int endIndex = Math.Max(selectableElement.Index, _selectionStart.Index);
            //        for (int i = startIndex; i <= endIndex; ++i)
            //        {
            //            AddRect(_selectableElements[i]);
            //        }
            //        break;
            //    }
            //}
        }

        private void ItemsControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            NewRegionMoved?.Invoke(sender, e);
        }

        private BoundsExtractionStrategy.SelectableElement _selectionStart;
        private double _pageSpacing;

        private void ItemsControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            NewRegionStarted?.Invoke(sender, e);
            //var pos = e.GetCurrentPoint(PageItemsControl).Position;
            //var ratio = _maxPdfPageWidth / PageItemsControl.ActualWidth;
            //pos.X = pos.X * ratio;
            //pos.Y = pos.Y * ratio;
            //foreach (var selectableElement in _selectableElements)
            //{
            //    if (selectableElement.Bounds.Contains(pos))
            //    {
            //        _selectionStart = selectableElement;
            //        break;
            //    }
            //}
        }

        private void AddRect(BoundsExtractionStrategy.SelectableElement element)
        {
            Rectangle r = new Rectangle
            {
                Width = element.Bounds.Width,
                Height = element.Bounds.Height,
            };
            Canvas.SetLeft(r, element.Bounds.X);
            Canvas.SetTop(r, element.Bounds.Y);
            r.Fill = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
            TestSelectionCanvas.Children.Add(r);
        }

        public event PointerEventHandler NewRegionStarted;
        public event PointerEventHandler NewRegionMoved;
        public event PointerEventHandler NewRegionEnded;

        private void updatePageSpacingAndScrollPosition()
        {
            var ratio = PageItemsControl.ActualWidth / _maxPdfPageWidth;
            PageSpacing = _pdfPageSpacing * ratio;
            var scrollPos = _scrollRatio * TestSelectionCanvas.Height * PageItemsControl.ActualWidth / _maxPdfPageWidth;
            ScrollViewer.ChangeView(null, scrollPos, null, true);
        }

        private void CustomPdfView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            updatePageSpacingAndScrollPosition();
        }

        private void ScrollViewer_OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            _scrollRatio = e.FinalView.VerticalOffset / (TestSelectionCanvas.Height * PageItemsControl.ActualWidth /  _maxPdfPageWidth); 
            LayoutDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, _scrollRatio, true);
        }

        private void ScrollViewer_OnLayoutUpdated(object sender, object e)
        {
            if (ScrollViewer.ExtentHeight != 0 && !double.IsNaN((TestSelectionCanvas.Height)))
            {
                ScrollViewer.LayoutUpdated -= ScrollViewer_OnLayoutUpdated;
                _scrollRatio = LayoutDocument.GetField<NumberController>(KeyStore.PdfVOffsetFieldKey)?.Data ?? 0;
                updatePageSpacingAndScrollPosition();
                Control.SizeChanged += CustomPdfView_OnSizeChanged;
            }
        }

        public async void UnFreeze()
        {
            await RenderPdf(PageItemsControl.ActualWidth);
        }
    }
}
