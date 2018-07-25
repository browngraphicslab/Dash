using System;
using System.Collections;
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
using Microsoft.Toolkit.Uwp.UI.Animations;
using Syncfusion.UI.Xaml.Controls;
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

        private DataVirtualizationSource<ImageSource> _pages1;
        public DataVirtualizationSource<ImageSource> Pages1
        {
            get => _pages1;
            set
            {
                _pages1 = value;
                OnPropertyChanged();
            }
        }

        private DataVirtualizationSource<ImageSource> _pages2;

        public DataVirtualizationSource<ImageSource> Pages2
        {
            get => _pages2;
            set
            {
                _pages2 = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<DocumentView> _annotationList = new ObservableCollection<DocumentView>();

        public ObservableCollection<DocumentView> Annotations
        {
            get => _annotationList;
            set
            {
                _annotationList = value;
                OnPropertyChanged();
            }
        }


        private List<DocumentController> _docControllers = new List<DocumentController>();

        public List<DocumentController> DocControllers
        {
            get => _docControllers;
            set
            {
                _docControllers = value;
                OnPropertyChanged();
            }
        }

        private List<Size> Tops;


        // we store section of selected text in this list of KVPs with the key and value as start and end index, respectively
        private readonly List<KeyValuePair<int, int>> _currentSelections = new List<KeyValuePair<int, int>>();

        public DocumentController LayoutDocument { get; }
        public DocumentController DataDocument { get; }

        //This makes the assumption that both pdf views are always in the same annotation mode
        public AnnotationType CurrentAnnotationType => _bottomAnnotationOverlay.AnnotationType;

        private WPdf.PdfDocument _wPdfDocument;
        private PDFRegionMarker _currentMarker;

        private Stack<double> _backStack;
        private Stack<double> _backStack2;

        private DispatcherTimer _timer;

        public WPdf.PdfDocument PDFdoc => _wPdfDocument;

        public CustomPdfView()
        {
            this.InitializeComponent();

        }
        
        private readonly NewAnnotationOverlay _topAnnotationOverlay;
        private readonly NewAnnotationOverlay _bottomAnnotationOverlay;

       

        public CustomPdfView(DocumentController document)
        {
            this.InitializeComponent();
            LayoutDocument = document.GetActiveLayout() ?? document;
            DataDocument = document.GetDataDocument();
            _pages1 = new DataVirtualizationSource<ImageSource>(this, TopScrollViewer, PageItemsControl);
            _pages2 = new DataVirtualizationSource<ImageSource>(this, BottomScrollViewer, PageItemsControl2);
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

            _bottomAnnotationOverlay = new NewAnnotationOverlay(LayoutDocument, RegionGetter);
            _topAnnotationOverlay = new NewAnnotationOverlay(LayoutDocument, RegionGetter);
            xTopPdfGrid.Children.Add(_topAnnotationOverlay);
            xBottomPdfGrid.Children.Add(_bottomAnnotationOverlay);

            BottomScrollViewer.SizeChanged += (ss, ee) =>
            {
                if (xBar.Width != 0)
                {
                    xBar.Width = BottomScrollViewer.ExtentWidth;
                }
            };

            xPdfContainer.SizeChanged += (ss, ee) =>
            {

                if (xFirstPanelRow.ActualHeight > xPdfContainer.ActualHeight - 5)
                {
                    if (xPdfContainer.ActualHeight - 5 > 0)
                    {
                        xFirstPanelRow.Height = new GridLength(xPdfContainer.ActualHeight - 4, GridUnitType.Pixel);
                    }
                   
                    xFirstPanelRow.MaxHeight = xPdfContainer.ActualHeight;

                }
                else
                {
                    xFirstPanelRow.MaxHeight = xPdfContainer.ActualHeight;
                }
            };

            _backStack = new Stack<double>();
            _backStack.Push(0);
            _backStack2 = new Stack<double>();
            _backStack2.Push(0);

            _timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 1000)
            };
            _timer.Tick += TimerOnTick;

            Canvas.SetZIndex(xButtonPanel2, 999);
            Canvas.SetZIndex(xButtonPanel, 999);

        }

        public void SetAnnotationType(AnnotationType type)
        {
            _bottomAnnotationOverlay.SetAnnotationType(type);
            _topAnnotationOverlay.SetAnnotationType(type);
        }

        //private void OnNewRegionMade(object sender, RegionEventArgs e)
        //{
        //    MakeRegionMarker(TopScrollViewer.VerticalOffset, e.Link);
        //}
	    
	    // adds to the side of the PDFView
	    private void MakeRegionMarker(double offset, DocumentController dc)
	    {
		    var newMarker = new PDFRegionMarker();
		    newMarker.SetScrollPosition(offset, TopScrollViewer.ExtentHeight);
		    newMarker.LinkTo = dc;
		    newMarker.Offset = offset;
		    newMarker.PointerPressed += xMarker_OnPointerPressed;
		    xAnnotationMarkers.Children.Add(newMarker);
		    _markers.Add(newMarker);
	        
		    xAnnotationMarkers.Visibility = Visibility.Visible;
	    }

        //private void OnRegionRemoved(object sender, RegionEventArgs e)
        //{
        //    foreach (var child in xAnnotationMarkers.Children.ToList())
        //    {
        //        if (child is PDFRegionMarker box)
        //        {
        //            if (box.LinkTo.Equals(e.Link))
        //            {
        //                xAnnotationMarkers.Children.Remove(child);
        //                _markers.Remove(box);

        //                if (_markers.Count == 0)
        //                {
        //                    xAnnotationMarkers.Visibility = Visibility.Collapsed;
        //                }
        //            }
        //        }
        //    }
        //}

        public DocumentController GetRegionDocument()
        {
            return _bottomAnnotationOverlay.GetRegionDoc();
        }

        private static DocumentController RegionGetter(AnnotationType type)
        {
            return new RichTextNote().Document;
        }

        //This might be more efficient as a linked list of KV pairs if our selections are always going to be contiguous
        private Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();
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

            var reader = new PdfReader(await file.OpenStreamForReadAsync());
            var pdfDocument = new PdfDocument(reader);
            var strategy = new BoundsExtractionStrategy();
            Strategy = strategy;
            var processor = new PdfCanvasProcessor(strategy);
            double offset = 0;
            double maxWidth = 0;
            for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
            {
                var page = pdfDocument.GetPage(i);
                Pages1.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
                Pages2.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
                maxWidth = Math.Max(maxWidth, page.GetPageSize().GetWidth());
            }

            PdfMaxWidth = maxWidth;

            _wPdfDocument = await WPdf.PdfDocument.LoadFromFileAsync(file);
            bool add = _wPdfDocument.PageCount != _currentPageCount;
            if (add)
            {
                _currentPageCount = (int)_wPdfDocument.PageCount;
            }

            await Task.Run(() =>
            {
                for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
                {
                    var page = pdfDocument.GetPage(i);
                    var size = page.GetPageSize();
                    strategy.SetPage(i - 1, offset, size);
                    offset += page.GetPageSize().GetHeight() + 10;
                    processor.ProcessPageContent(page);
                }
            });
            
            var selectableElements = strategy.GetSelectableElements(0, pdfDocument.GetNumberOfPages() - 1);
            _topAnnotationOverlay.SetSelectableElements(selectableElements);
            _bottomAnnotationOverlay.SetSelectableElements(selectableElements);

            reader.Close();
            pdfDocument.Close();
            PdfTotalHeight = offset - 10;
            DocumentLoaded?.Invoke(this, new EventArgs());
		}

        public BoundsExtractionStrategy Strategy { get; set; }

        private CancellationTokenSource _renderToken;
        private int _currentPageCount = -1;

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

        private void XPdfGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

            //    if (AnnotationManager.CurrentAnnotationType.Equals(Dash.AnnotationManager.AnnotationType.TextSelection))
            //    {
            //        var mouse = new Point(e.GetPosition(xPdfGrid).X, e.GetPosition(xPdfGrid).Y);
            //        var closest = GetClosestElementInDirection(mouse, mouse);

            //        //space, tab, enter

            //        if ((Math.Abs(closest.Bounds.X - mouse.X) < 10) && Math.Abs(closest.Bounds.Y - mouse.Y) < 10)
            //        {
            //            SelectIndex(closest.Index);
            //        }



            //        for (var i = closest.Index; i >= 0; --i)
            //        {
            //            var selectableElement = SelectableElements[i];
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
            //            var selectableElement = SelectableElements[i];
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

        }

        #region Region/Selection Events

        private void XPdfGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if(currentPoint.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased)
            {
                return;
            }

            var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
            overlay.EndAnnotation(e.GetCurrentPoint(overlay).Position);
            e.Handled = true;
        }

        private void XPdfGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
            if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                overlay.EndAnnotation(e.GetCurrentPoint(overlay).Position);
                return;
            }
            if (!currentPoint.Properties.IsLeftButtonPressed)
            {
                return;
            }
            overlay.UpdateAnnotation(e.GetCurrentPoint(overlay).Position);

            //e.Handled = true;
        }

        private void XPdfGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
            if (currentPoint.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }

            overlay.StartAnnotation(e.GetCurrentPoint(overlay).Position);
            e.Handled = true;
        }

        #endregion

        // ScrollViewers don't deal well with being resized so we have to manually track the scroll ratio and restore it on SizeChanged
        private double _scrollRatio;
        private double _height;
        private double _width;
        private double _verticalOffset;
        private bool _isCtrlPressed;

        public void CustomPdfView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            TopScrollViewer.ChangeView(null, _scrollRatio * TopScrollViewer.ExtentHeight, null, true);
        }

        private void ScrollViewer_OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            _scrollRatio = e.FinalView.VerticalOffset / TopScrollViewer.ExtentHeight;
            LayoutDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, _scrollRatio, true);
        }

        public void UnFreeze()
        {
            //await RenderPdf(ScrollViewer.ActualWidth);
            Pages1.View_SizeChanged();
            Pages2.View_SizeChanged();
        }

        private void CustomPdfView_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            //if (this.IsCtrlPressed())
            //{
            //    if (e.Key == VirtualKey.C && _currentSelections.Last().Key != -1)
            //    {
            //        Debug.Assert(_currentSelections.Last().Value != -1);
            //        Debug.Assert(_currentSelections.Last().Value >= _currentSelections.Last().Key);
            //        StringBuilder sb = new StringBuilder();
            //        _currentSelections.Sort((s1, s2) => Math.Sign(s1.Key - s2.Key));

            //        // get the indices from our selections and ignore any duplicate selections
            //        var indices = new List<int>();
            //        foreach (var selection in _currentSelections)
            //        {
            //            for (var i = selection.Key; i <= selection.Value; i++)
            //            {
            //                if (!indices.Contains(i))
            //                {
            //                    indices.Add(i);
            //                }
            //            }
            //        }

            //        // if there's ever a jump in our indices, insert two line breaks before adding the next index
            //        var prevIndex = indices.First();
            //        foreach (var index in indices.Skip(1))
            //        {
            //            if (prevIndex + 1 != index)
            //            {
            //                sb.Append("\r\n\r\n");
            //            }
            //            var selectableElement = SelectableElements[index];
            //            if (selectableElement.Type == SelectableElement.ElementType.Text)
            //            {
            //                sb.Append((string)selectableElement.Contents);
            //            }

            //            prevIndex = index;
            //        }
                    
            //        var dataPackage = new DataPackage();
            //        dataPackage.SetText(sb.ToString());
            //        Clipboard.SetContent(dataPackage);
            //        e.Handled = true;
            //    }
            //    else if (e.Key == VirtualKey.A)
            //    {
            //        SelectElements(0, SelectableElements.Count - 1);
            //        e.Handled = true;
            //    }
            //}
        }

        public void ScrollToRegion(DocumentController target)
        {
            var offset = target.GetDataDocument().GetPosition()?.Y;
            if (offset == null) return;

            TopScrollViewer.ChangeView(null, offset, null);
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
                TopScrollViewer.ChangeView(null, region.Offset, null);
            }
        }
        private void xNextAnnotation_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currOffset = TopScrollViewer.VerticalOffset;
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
            var currOffset = TopScrollViewer.VerticalOffset;
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

		public void DisplayFlyout(MenuFlyout linkFlyout)
		{
			linkFlyout.ShowAt(this);
		}

       
        private void XAnnotationBox_OnTapped(object sender, TappedRoutedEventArgs e)
        {
           
            var region = GetRegionDocument();
            // note is the new annotation textbox that is created
            var note = new RichTextNote("<annotation>", new Point(), new Size(xAnnotationBox.Width, double.NaN)).Document;

            region.Link(note);
            var docview = new DocumentView
            {
                DataContext = new DocumentViewModel(note) {DecorationState = false},
                Width = xAnnotationBox.ActualWidth,
                BindRenderTransform = false
            };
            docview.hideResizers();

            Canvas.SetTop(docview, region.GetDataDocument().GetPosition()?.Y ?? 0);
            //SetAnnotationPosition(ScrollViewer.VerticalOffset, docview);
            Annotations.Add(docview);
            DocControllers.Add(docview.ViewModel.LayoutDocument);            //if(AnnotationManager.CurrentAnnotationType.Equals(AnnotationManager.AnnotationType.RegionBox))
            DataDocument.SetField(KeyStore.AnnotationsKey, new ListController<DocumentController>(DocControllers), true);

        }

        private void XAnnotationsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (xAnnotationBox.Width.Equals(0))
            {
                //xAnnotationBox.Visibility = Visibility.Visible;
                //xAnnotationBox2.Visibility = Visibility.Visible;
                xAnnotationBox.Width = 200;
                xAnnotationBox2.Width = 200;
            
            }
            else
            {
                //xAnnotationBox.Visibility = Visibility.Collapsed;
                //xAnnotationBox2.Visibility = Visibility.Collapsed;
                xAnnotationBox.Width = 0;
                xAnnotationBox2.Width = 0;
            }
        }

       

        private void XPdfDivider_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            
            e.Handled = true;
        }


        private void XPdfDivider_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (this.IsRightBtnPressed())
            {
                e.Complete();
            }

            e.Handled = true;
        }

        private void XPdfDivider_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void XPdfDivider_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void XScrollToTop2_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            BottomScrollViewer.ChangeView(null, 0, null);
        }

        private void XNextPageButton2_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PageNext(BottomScrollViewer);

        }

        private void XPreviousPageButton2_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PagePrev(BottomScrollViewer);
        }

        private void XScrollBack2_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopStack(_backStack2, BottomScrollViewer);
        }

        private void ScrollViewer2_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
            _timer.Start();
        }

        private void TimerOnTick(object o, object o1)
        {
            _timer.Stop();
            AddToStack(_backStack, TopScrollViewer);
            AddToStack(_backStack2, BottomScrollViewer);

        }

        private void XNextPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PageNext(TopScrollViewer);
        }

        private void XPreviousPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PagePrev(TopScrollViewer);
        }

        private void XScrollToTop_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            TopScrollViewer.ChangeView(null, 0, null);
        }

        private void XScrollBack_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopStack(_backStack, TopScrollViewer);
        }

     
        //private void MovePage(ScrollViewer scroller, Grid grid, int i)
        //{
        //    var currOffset = 0.0;
        //    foreach (var image in PageItemsControl.GetDescendantsOfType<Image>())
        //    {
        //        var imgWidth = image.ActualWidth;
        //        var annoWidth = grid.Visibility == Visibility.Visible ? grid.ActualWidth : 0;
        //        var scale = (scroller.ViewportWidth - annoWidth) / imgWidth;
        //        if (i == 0)
        //        {
        //            if (currOffset + (image.ActualHeight * scale) + 5 > scroller.VerticalOffset)
        //            {
        //                break;
        //            }
        //            currOffset += (image.ActualHeight * scale);
        //        }
        //        else
        //        {
        //            currOffset += (image.ActualHeight * scale);
        //            if (currOffset > scroller.VerticalOffset + 5)
        //            {
        //                break;
        //            }
        //        }
                
        //    }
        //    scroller.ChangeView(null, currOffset, 1);
        //}

        private void PagePrev(ScrollViewer scroller)
        {
            DataVirtualizationSource<ImageSource> pages;
            double annoWidth = 0;
            if (scroller.Equals(TopScrollViewer))
            {
                pages = _pages1;
                annoWidth = xAnnotationBox.Width;
            }

            else
            {
                pages = _pages2;
                annoWidth = xAnnotationBox2.Width;
            }

            var sizes = pages.PageSizes;
            var currOffset = 0.0;
            foreach (var size in sizes)
            {
                var scale = (scroller.ViewportWidth - annoWidth) / size.Width;
                if (currOffset + (size.Height * scale) + 15 - scroller.VerticalOffset >= 0)
                {
                    break;
                }

                currOffset += (size.Height * scale) + 15;
               

            }

            scroller.ChangeView(null, currOffset, 1);
        }

        private void PageNext(ScrollViewer scroller)
        {
            DataVirtualizationSource<ImageSource> pages;
            double annoWidth;
            if (scroller.Equals(TopScrollViewer))
            {
                pages = _pages1;
                annoWidth = xAnnotationBox.Width;
            }

            else
            {
                pages = _pages2;
                annoWidth = xAnnotationBox2.Width;
            }

            var sizes = pages.PageSizes;
            var currOffset = 0.0;
            foreach (var size in sizes)
            {
                var scale = (scroller.ViewportWidth - annoWidth) / size.Width;
                currOffset += (size.Height * scale) + 15;
                if (currOffset - scroller.VerticalOffset > 1)
                {
                    break;
                }

            }

            scroller.ChangeView(null, currOffset, 1);
        }

        private void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
            _timer.Start();
        }

        private void AddToStack(Stack<double> stack, ScrollViewer viewer)
        {
            if (!stack.Count().Equals(0))
            {
                if (!stack.Peek().Equals(viewer.VerticalOffset))
                {
                    stack.Push(viewer.VerticalOffset);
                }
            }
        }

        private void PopStack(Stack<double> stack, ScrollViewer viewer)
        {
            if (stack.Count >= 1)
            {
                viewer.ChangeView(null, stack.Peek(), 1);
                stack.Pop();
            }
        }

        private void XControlsToggleButton2_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (xToggleButtonStack2.Visibility.Equals(Visibility.Collapsed))
            {
                xToggleButtonStack2.Visibility = Visibility.Visible;
                xFadeAnimation2.Begin();
            }
            else
            {
                xToggleButtonStack2.Visibility = Visibility.Collapsed;
            }
           
        }

        private void XControlsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (xToggleButtonStack.Visibility.Equals(Visibility.Collapsed))
            {
                xToggleButtonStack.Visibility = Visibility.Visible;
                xFadeAnimation.Begin();
            }
            else
            {
                xToggleButtonStack.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowRegions()
        {
            _topAnnotationOverlay.AnnotationVisibility = true;
            _bottomAnnotationOverlay.AnnotationVisibility = true;
        }

        public void HideRegions()
        {
            _topAnnotationOverlay.AnnotationVisibility = false;
            _bottomAnnotationOverlay.AnnotationVisibility = false;
        }

        public bool AreAnnotationsVisible()
        {
            //This makes the assumption that both overlays are kept in sync
            return _bottomAnnotationOverlay.AnnotationVisibility;
        }
    }
}

