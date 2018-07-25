using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
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
    public sealed partial class CustomPdfView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty PdfUriProperty = DependencyProperty.Register(
            "PdfUri", typeof(Uri), typeof(CustomPdfView), new PropertyMetadata(default(Uri), PropertyChangedCallback));

        private List<PDFRegionMarker> _markers = new List<PDFRegionMarker>();

        public Uri PdfUri
        {
            get => (Uri)GetValue(PdfUriProperty);
            set => SetValue(PdfUriProperty, value);
        }

        private double _pdfMaxWidth;
        public double PdfMaxWidth
        {
            get => _pdfMaxWidth;
            set
            {
                _pdfMaxWidth = value;
                OnPropertyChanged();
            }
        }

        private double _pdfTotalHeight;
        public double PdfTotalHeight
        {
            get => _pdfTotalHeight;
            set
            {
                _pdfTotalHeight = value;
                OnPropertyChanged();
            }
        }

        public event EventHandler DocumentLoaded;

        private ObservableCollection<ImageSource> _pages = new ObservableCollection<ImageSource>();
        public ObservableCollection<ImageSource> Pages
        {
            get => _pages;
            set
            {
                _pages = value;
                OnPropertyChanged();
            }
        }

        public DocumentController LayoutDocument { get; }
        public DocumentController DataDocument { get; }
        public AnnotationType CurrentAnnotationType => _annotationOverlay.AnnotationType;

        private WPdf.PdfDocument _wPdfDocument;

        private readonly NewAnnotationOverlay _annotationOverlay;

        public CustomPdfView(DocumentController document)
        {
            this.InitializeComponent();
            LayoutDocument = document.GetActiveLayout() ?? document;
            DataDocument = document.GetDataDocument();
            //DocumentLoaded += (sender, e) =>
            //{
            //    AnnotationManager.NewRegionMade += OnNewRegionMade;
            //    AnnotationManager.RegionRemoved += OnRegionRemoved;

            //    var dataRegions = DataDocument.GetDataDocument()
            //        .GetField<ListController<DocumentController>>(KeyStore.RegionsKey);
            //    if (dataRegions != null)
            //    {
            //        the VisualAnnotationManager will take care of the regioning, but here we need to put on the side markers on
            //        xAnnotations.Height = PdfTotalHeight;
            //        foreach (var region in dataRegions.TypedData)
            //        {
            //            var offset = region.GetDataDocument().GetField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey).Data;
            //            MakeRegionMarker(offset, region);
            //        }
            //    }

            //};
            //AnnotationManager = new VisualAnnotationManager(this, LayoutDocument, xAnnotations);

            _annotationOverlay = new NewAnnotationOverlay(LayoutDocument, RegionGetter);
            xPdfGrid.Children.Add(_annotationOverlay);
        }

        public void SetAnnotationType(AnnotationType type)
        {
            _annotationOverlay.SetAnnotationType(type);
        }

        private void OnNewRegionMade(object sender, RegionEventArgs e)
        {
            MakeRegionMarker(ScrollViewer.VerticalOffset, e.Link);
        }

        // adds to the side of the PDFView
        private void MakeRegionMarker(double offset, DocumentController dc)
        {
            var newMarker = new PDFRegionMarker();
            newMarker.SetScrollPosition(offset, ScrollViewer.ExtentHeight);
            newMarker.LinkTo = dc;
            newMarker.Offset = offset;
            newMarker.PointerPressed += xMarker_OnPointerPressed;
            xAnnotationMarkers.Children.Add(newMarker);
            _markers.Add(newMarker);
            xAnnotationMarkers.Visibility = Visibility.Visible;
        }

        private void OnRegionRemoved(object sender, RegionEventArgs e)
        {
            foreach (var child in xAnnotationMarkers.Children.ToList())
            {
                if (child is PDFRegionMarker box)
                {
                    if (box.LinkTo.Equals(e.Link))
                    {
                        xAnnotationMarkers.Children.Remove(child);
                        _markers.Remove(box);

                        if (_markers.Count == 0)
                        {
                            xAnnotationMarkers.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        public DocumentController GetRegionDocument()
        {
            return _annotationOverlay.GetRegionDoc();
        }

        private DocumentController RegionGetter(AnnotationType type)
        {
            return new RichTextNote().Document;
        }

        private async Task OnPdfUriChanged()
        {
            if (PdfUri == null)
            {
                return;
            }
            Pages.Clear();

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

            PdfReader reader = new PdfReader(await file.OpenStreamForReadAsync());
            var pdfDocument = new PdfDocument(reader);
            var strategy = new BoundsExtractionStrategy();
            var processor = new PdfCanvasProcessor(strategy);
            double offset = 0;
            double maxWidth = 0;
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
            {
                var page = pdfDocument.GetPage(i);
                var size = page.GetPageSize();
                maxWidth = Math.Max(maxWidth, size.GetWidth());
                strategy.SetPage(i - 1, offset, size);
                offset += page.GetPageSize().GetHeight() + 10;
                processor.ProcessPageContent(page);
            }

            PdfMaxWidth = maxWidth;
            PdfTotalHeight = offset - 10;

            _annotationOverlay.SetSelectableElements(strategy.GetSelectableElements());
            reader.Close();
            pdfDocument.Close();

            _wPdfDocument = await WPdf.PdfDocument.LoadFromFileAsync(file);
            await RenderPdf(null);

            var scrollRatio = LayoutDocument.GetField<NumberController>(KeyStore.PdfVOffsetFieldKey);
            if (scrollRatio != null)
            {
                ScrollViewer.UpdateLayout();
                ScrollViewer.ChangeView(null, scrollRatio.Data * ScrollViewer.ExtentHeight, null, true);
            }
            DocumentLoaded?.Invoke(this, new EventArgs());
        }

        private CancellationTokenSource _renderToken;
        private int _currentPageCount = -1;
        private async Task RenderPdf(double? targetWidth)
        {
            _renderToken?.Cancel();
            _renderToken = new CancellationTokenSource();
            CancellationToken token = _renderToken.Token;
            var options = new WPdf.PdfPageRenderOptions();
            bool add = _wPdfDocument.PageCount != _currentPageCount;
            if (add)
            {
                _currentPageCount = (int)_wPdfDocument.PageCount;
                Pages.Clear();
            }
            for (uint i = 0; i < _wPdfDocument.PageCount; ++i)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                var stream = new InMemoryRandomAccessStream();
                var widthRatio = targetWidth == null ? (ActualWidth == 0 ? 1 : (ActualWidth / PdfMaxWidth)) : (targetWidth / PdfMaxWidth);
                options.DestinationWidth = (uint)(widthRatio * _wPdfDocument.GetPage(i).Dimensions.MediaBox.Width);
                options.DestinationHeight = (uint)(widthRatio * _wPdfDocument.GetPage(i).Dimensions.MediaBox.Height);
                await _wPdfDocument.GetPage(i).RenderToStreamAsync(stream, options);
                var source = new BitmapImage();
                await source.SetSourceAsync(stream);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if ((int)i < Pages.Count)
                {
                    Pages[(int)i] = source;
                }
                else
                {
                    Pages.Add(source);
                }
            }
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
            //AnnotationManager?.RegionSelected(region, pt, chosenDoc);
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

        public DocumentController GetDocControllerFromSelectedRegion(AnnotationType annotationType)
        {
            var dc = new RichTextNote("PDF " + ScrollViewer.VerticalOffset).Document;
            dc.GetDataDocument().SetField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey, ScrollViewer.VerticalOffset, true);
            dc.SetRegionDefinition(LayoutDocument);
            dc.SetAnnotationType(annotationType);

            return dc;
        }

        //private void XPdfGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        //{

        //    if (false) //AnnotationManager.CurrentAnnotationType.Equals(Dash.AnnotationManager.AnnotationType.TextSelection))
        //    {
        //        var mouse = new Point(e.GetPosition(xPdfGrid).X, e.GetPosition(xPdfGrid).Y);
        //        var closest = GetClosestElementInDirection(mouse, mouse);

        //        //space, tab, enter

        //        if ((Math.Abs(closest.Bounds.X - mouse.X) < 10) && (Math.Abs(closest.Bounds.Y - mouse.Y) < 10))
        //        {
        //            SelectIndex(closest.Index);
        //        }



        //        for (var i = closest.Index; i >= 0; --i)
        //        {
        //            var selectableElement = _selectableElements[i];
        //            if (!selectableElement.Contents.ToString().Equals(" ") && !selectableElement.Contents.ToString().Equals("\t") && !selectableElement.Contents.ToString().Equals("\n"))
        //            {
        //                SelectIndex(selectableElement.Index);
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }

        //        for (var i = closest.Index; i >= 0; ++i)
        //        {
        //            var selectableElement = _selectableElements[i];
        //            if (!selectableElement.Contents.ToString().Equals(" ") && !selectableElement.Contents.ToString().Equals("\t") && !selectableElement.Contents.ToString().Equals("\n"))
        //            {
        //                SelectIndex(selectableElement.Index);
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }
        //    }

        //}

        #region Region/Selection Events

        private void XPdfGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if(currentPoint.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased)
            {
                return;
            }
            _annotationOverlay.EndAnnotation(e.GetCurrentPoint(_annotationOverlay).Position);
        }

        private void XPdfGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                _annotationOverlay.EndAnnotation(e.GetCurrentPoint(_annotationOverlay).Position);
                return;
            }
            if (!currentPoint.Properties.IsLeftButtonPressed)
            {
                return;
            }
            _annotationOverlay.UpdateAnnotation(e.GetCurrentPoint(_annotationOverlay).Position);
        }

        private void XPdfGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if (currentPoint.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }

            _annotationOverlay.StartAnnotation(e.GetCurrentPoint(_annotationOverlay).Position);
        }

        #endregion

        // ScrollViewers don't deal well with being resized so we have to manually track the scroll ratio and restore it on SizeChanged
        private double _scrollRatio;
        private void CustomPdfView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScrollViewer.ChangeView(null, _scrollRatio * ScrollViewer.ExtentHeight, null, true);
        }

        private void ScrollViewer_OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            _scrollRatio = e.FinalView.VerticalOffset / ScrollViewer.ExtentHeight;
            LayoutDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, _scrollRatio, true);
        }

        public async void UnFreeze()
        {
            await RenderPdf(ScrollViewer.ActualWidth);
        }

        private void CustomPdfView_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            //if (this.IsCtrlPressed())
            //{
            //    if (e.Key == VirtualKey.C && _currentSelectionStart != -1)
            //    {
            //        Debug.Assert(_currentSelectionEnd != -1);
            //        Debug.Assert(_currentSelectionEnd >= _currentSelectionStart);
            //        StringBuilder sb = new StringBuilder();
            //        for (var i = _currentSelectionStart; i <= _currentSelectionEnd; ++i)
            //        {
            //            var selectableElement = _selectableElements[i];
            //            if (selectableElement.Type == SelectableElement.ElementType.Text)
            //            {
            //                sb.Append((string)selectableElement.Contents);
            //            }
            //        }
            //        var dataPackage = new DataPackage();
            //        dataPackage.SetText(sb.ToString());
            //        Clipboard.SetContent(dataPackage);
            //        e.Handled = true;
            //    }
            //    else if (e.Key == VirtualKey.A)
            //    {
            //        SelectElements(0, _selectableElements.Count - 1);
            //        e.Handled = true;
            //    }
            //}
        }

        public void ScrollToRegion(DocumentController target)
        {
            var offset = target.GetDataDocument().GetPosition()?.Y;
            if (offset == null) return;

            ScrollViewer.ChangeView(null, offset, null);
        }

        // when the sidebar marker gets pressed
        private void xMarker_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //MarkerSelected((PDFRegionMarker)sender);
            //AnnotationManager.SelectRegion(((PDFRegionMarker)sender).LinkTo);
            //e.Handled = true;
        }

        // moves to the region's offset
        private void MarkerSelected(PDFRegionMarker region)
        {
            if (region != null)
            {
                // todo: do we need the zoom factor multiplied by offset here?
                ScrollViewer.ChangeView(null, region.Offset, null);
            }
        }
        private void xNextAnnotation_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currOffset = ScrollViewer.VerticalOffset;
            PDFRegionMarker nextOffset = null;

            foreach (var region in _markers)
            {
                if (region.Offset > currOffset && Math.Abs(region.Offset - currOffset) > 1 && (nextOffset == null || region.Offset < nextOffset.Offset))
                {
                    nextOffset = region;
                }
            }
            MarkerSelected(nextOffset);
        }

        private void xPrevAnnotation_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currOffset = ScrollViewer.VerticalOffset;
            PDFRegionMarker prevOffset = null;

            foreach (var region in _markers)
            {
                if (region.Offset < currOffset && Math.Abs(region.Offset - currOffset) > 1 && (prevOffset == null || region.Offset > prevOffset.Offset))
                {
                    prevOffset = region;
                }
            }
            MarkerSelected(prevOffset);
        }

        private void Scrollviewer_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_markers.Count > 0) xAnnotationNavigation.Opacity = 0.8;
        }

        private void Scrollviewer_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            xAnnotationNavigation.Opacity = 0;
        }

        public void ShowRegions()
        {
            _annotationOverlay.AnnotationVisibility = true;
        }

        public void HideRegions()
        {
            _annotationOverlay.AnnotationVisibility = false;
        }

        public bool AreAnnotationsVisible()
        {
            return _annotationOverlay.AnnotationVisibility;
        }
    }
}

