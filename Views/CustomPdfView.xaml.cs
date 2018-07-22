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
    public sealed partial class CustomPdfView : UserControl, INotifyPropertyChanged, IVisualAnnotatable
    {
        public static readonly DependencyProperty PdfUriProperty = DependencyProperty.Register(
            "PdfUri", typeof(Uri), typeof(CustomPdfView), new PropertyMetadata(default(Uri), PropertyChangedCallback));

	    private List<PDFRegionMarker> _markers = new List<PDFRegionMarker>();

		public Uri PdfUri
        {
            get { return (Uri)GetValue(PdfUriProperty); }
            set { SetValue(PdfUriProperty, value); }
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


        private List<SelectableElement> _selectableElements;

        public VisualAnnotationManager AnnotationManager { get; }

        public DocumentController LayoutDocument { get; }
        public DocumentController DataDocument { get; }

        private WPdf.PdfDocument _wPdfDocument;
        private PDFRegionMarker _currentMarker;

        public CustomPdfView()
        {
            this.InitializeComponent();

        }

       

        public CustomPdfView(DocumentController document)
        {


            this.InitializeComponent();
            LayoutDocument = document.GetActiveLayout() ?? document;
            DataDocument = document.GetDataDocument();
			DocumentLoaded += (sender, e) =>
			{
				AnnotationManager.NewRegionMade += OnNewRegionMade;
				AnnotationManager.RegionRemoved += OnRegionRemoved;

				var dataRegions = DataDocument.GetDataDocument()
					.GetField<ListController<DocumentController>>(KeyStore.RegionsKey);
				if (dataRegions != null)
				{
					// the VisualAnnotationManager will take care of the regioning, but here we need to put on the side markers on
					xAnnotations.Height = PdfTotalHeight;
					foreach (var region in dataRegions.TypedData)
					{
						var offset = region.GetDataDocument().GetField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey).Data;
						MakeRegionMarker(offset, region);
					}
				}

			    var dataAnnotations = DataDocument.GetDataDocument()
			        .GetField<ListController<DocumentController>>(KeyStore.AnnotationsKey);
                if (dataAnnotations != null)
                {
                    // the VisualAnnotationManager will take care of the regioning, but here we need to put on the side markers on

                    foreach (var annotation in dataAnnotations)
                    {

                        var dmv = new DocumentViewModel(annotation);
                        dmv.DisableDecorations = true;
                        var docview = new DocumentView();
                        docview.ViewModel = dmv;
                        docview.hideResizers();
                        Annotations.Add(docview);
                    }
                }

            };
            AnnotationManager = new VisualAnnotationManager(this, LayoutDocument, xAnnotations);
        }
		private void OnNewRegionMade(object sender, RegionEventArgs e)
	    {
		    MakeRegionMarker(ScrollViewer.VerticalOffset, e.Link);
	        //var docview = new DocumentView();
            //var dvm = new DocumentViewModel(e.Link);
	        //docview.ViewModel = dvm;
	        //Annotations.Add(docview);

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

            var region = AnnotationManager?.GetRegionDocument();
            ClearSelection();
            return region;
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
            
            _selectableElements = strategy.GetSelectableElements();
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
            //targetWidth = 1400;//This makes the PDF readable even if you shrink it down and then zoom in on it
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
            AnnotationManager?.RegionSelected(region, pt, chosenDoc);
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

	    public VisualAnnotationManager GetAnnotationManager()
	    {
		    return AnnotationManager;
	    }

	    public DocumentController GetDocControllerFromSelectedRegion(AnnotationManager.AnnotationType annotationType)
        {
            var dc = new RichTextNote("PDF " + ScrollViewer.VerticalOffset).Document;
	        dc.GetDataDocument().SetField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey, ScrollViewer.VerticalOffset, true);
			dc.SetRegionDefinition(LayoutDocument, annotationType);

            return dc;
        }

        #region Selection

        private double GetMinRectDist(Rect r, Point p, out Point closest)
        {
            var x1Dist = p.X - r.Left;
            var x2Dist = p.X - r.Right;
            var y1Dist = p.Y - r.Top;
            var y2Dist = p.Y - r.Bottom;
            x1Dist *= x1Dist;
            x2Dist *= x2Dist;
            y1Dist *= y1Dist;
            y2Dist *= y2Dist;
            closest.X = x1Dist < x2Dist ? r.Left : r.Right;
            closest.Y = y1Dist < y2Dist ? r.Top : r.Bottom;
            return Math.Min(x1Dist, x2Dist) + Math.Min(y1Dist, y2Dist);
        }

        private SelectableElement GetClosestElementInDirection(Point p, Point dir)
        {
            SelectableElement ele = null;
            double closestDist = double.PositiveInfinity;
            foreach (var selectableElement in _selectableElements)
            {
                var b = selectableElement.Bounds;
                if (b.Contains(p))
                {
                    return selectableElement;
                }
                var dist = GetMinRectDist(b, p, out var closest);
                if (dist < closestDist && (closest.X - p.X) * dir.X + (closest.Y - p.Y) * dir.Y > 0)
                {
                    ele = selectableElement;
                    closestDist = dist;
                }
            }

            return ele;
        }

        private void DeselectIndex(int index)
        {
            if (!_selectedRectangles.ContainsKey(index))
            {
                return;
            }

            TestSelectionCanvas.Children.Remove(_selectedRectangles[index]);
            _selectedRectangles.Remove(index);
        }

        private readonly SolidColorBrush _selectionBrush = new SolidColorBrush(Color.FromArgb(120, 0x94, 0xA5, 0xBB));

        private void SelectIndex(int index)
        {
            if (_selectedRectangles.ContainsKey(index))
            {
                return;
            }

            var ele = _selectableElements[index];
            var rect = new Rectangle
            {
                Width = ele.Bounds.Width,
                Height = ele.Bounds.Height
            };
            Canvas.SetLeft(rect, ele.Bounds.Left);
            Canvas.SetTop(rect, ele.Bounds.Top);
            rect.Fill = _selectionBrush;

            TestSelectionCanvas.Children.Add(rect);

            _selectedRectangles[index] = rect;
        }


        //This might be more efficient as a linked list of KV pairs if our selections are always going to be contiguous
        private Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();
        private int _currentSelectionStart = -1, _currentSelectionEnd = -1;
        private void SelectElements(int startIndex, int endIndex)
        {
            if (_currentSelectionStart == -1)
            {
                Debug.Assert(_currentSelectionEnd == -1);
                for (var i = startIndex; i <= endIndex; ++i)
                {
                    SelectIndex(i);
                }
            }
            else
            {
                for (var i = startIndex; i < _currentSelectionStart; ++i)
                {
                    SelectIndex(i);
                }
                for (var i = _currentSelectionStart; i < startIndex; ++i)
                {
                    DeselectIndex(i);
                }
                for (var i = _currentSelectionEnd + 1; i <= endIndex; ++i)
                {
                    SelectIndex(i);
                }
                for (var i = endIndex + 1; i <= _currentSelectionEnd; ++i)
                {
                    DeselectIndex(i);
                }
            }

            _currentSelectionStart = startIndex;
            _currentSelectionEnd = endIndex;

        }

        private void UpdateSelection(Point mousePos)
        {
            if (_selectionStartPoint.HasValue)
            {
                if (Math.Abs(_selectionStartPoint.Value.X - mousePos.X) < 3 &&
                    Math.Abs(_selectionStartPoint.Value.Y - mousePos.Y) < 3)
                {
                    return;
                }
                var dir = new Point(mousePos.X - _selectionStartPoint.Value.X, mousePos.Y - _selectionStartPoint.Value.Y);
                var startEle = GetClosestElementInDirection(_selectionStartPoint.Value, dir);
                if (startEle == null)
                {
                    return;
                }
                var currentEle = GetClosestElementInDirection(mousePos, new Point(-dir.X, -dir.Y));
                if (currentEle == null)
                {
                    return;
                }
                SelectElements(Math.Min(startEle.Index, currentEle.Index), Math.Max(startEle.Index, currentEle.Index));
            }
        }

        private void ClearSelection()
        {
            _currentSelectionStart = -1;
            _currentSelectionEnd = -1;
            _selectionStartPoint = null;
            _selectedRectangles.Clear();
            TestSelectionCanvas.Children.Clear();
			AnnotationManager.SetSelectionRegion(null);
        }

        private void EndSelection()
		{
			if (_currentSelectionStart == -1) return;//Not currently selecting anything
			_selectionStartPoint = null;
            AnnotationManager.SetSelectionRegion(_selectableElements.Skip(_currentSelectionStart).Take(_currentSelectionEnd - _currentSelectionStart + 1));
        }

        private void XPdfGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

            if (AnnotationManager.CurrentAnnotationType.Equals(Dash.AnnotationManager.AnnotationType.TextSelection))
            {
                var mouse = new Point(e.GetPosition(xPdfGrid).X, e.GetPosition(xPdfGrid).Y);
                var closest = GetClosestElementInDirection(mouse, mouse);

                //space, tab, enter

                if ((Math.Abs(closest.Bounds.X - mouse.X) < 10) && (Math.Abs(closest.Bounds.Y - mouse.Y) < 10))
                {
                    SelectIndex(closest.Index);
                }



                for (var i = closest.Index; i >= 0; --i)
                {
                    var selectableElement = _selectableElements[i];
                    if (!selectableElement.Contents.ToString().Equals(" ") && !selectableElement.Contents.ToString().Equals("\t") && !selectableElement.Contents.ToString().Equals("\n"))
                    {
                        SelectIndex(selectableElement.Index);
                    }
                    else
                    {
                        break;
                    }
                }

                for (var i = closest.Index; i >= 0; ++i)
                {
                    var selectableElement = _selectableElements[i];
                    if (!selectableElement.Contents.ToString().Equals(" ") && !selectableElement.Contents.ToString().Equals("\t") && !selectableElement.Contents.ToString().Equals("\n"))
                    {
                        SelectIndex(selectableElement.Index);
                    }
                    else
                    {
                        break;
                    }
                }
            }
          
        }

        #endregion

        #region Region/Selection Events

        private void XPdfGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            switch (AnnotationManager.CurrentAnnotationType)
            {
                case Dash.AnnotationManager.AnnotationType.TextSelection:
                    var currentPoint = e.GetCurrentPoint(PageItemsControl);
                    var pos = currentPoint.Position;
                    UpdateSelection(pos);
                    EndSelection();
                    break;
            }
        }

        private void XPdfGrid_OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            EndSelection();
        }

        private void XPdfGrid_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            EndSelection();
        }

        private void XPdfGrid_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            EndSelection();
        }

        private void XPdfGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if (!currentPoint.Properties.IsLeftButtonPressed)
            {
                return;
            }
            switch (AnnotationManager.CurrentAnnotationType)
            {
                case Dash.AnnotationManager.AnnotationType.TextSelection:
                    var pos = currentPoint.Position;
                    UpdateSelection(pos);
                    break;
	            case Dash.AnnotationManager.AnnotationType.None:
		            break;
	            case Dash.AnnotationManager.AnnotationType.RegionBox:
		            break;
	            case Dash.AnnotationManager.AnnotationType.Ink:
		            break;
	            default:
		            throw new ArgumentOutOfRangeException();
            }
        }

        private Point? _selectionStartPoint;

        private void XPdfGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(PageItemsControl);
            if (!currentPoint.Properties.IsLeftButtonPressed)
            {
                return;
            }
            ClearSelection();
            switch (AnnotationManager.CurrentAnnotationType)
            {
                case Dash.AnnotationManager.AnnotationType.RegionBox:
                    NewRegionStarted?.Invoke(sender, e);
                    break;
                case Dash.AnnotationManager.AnnotationType.TextSelection:
                    var pos = currentPoint.Position;
                    _selectionStartPoint = pos;
                    break;
            }
        }

        #endregion

        public event PointerEventHandler NewRegionStarted;

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
            if (this.IsCtrlPressed())
            {
                if (e.Key == VirtualKey.C && _currentSelectionStart != -1)
                {
                    Debug.Assert(_currentSelectionEnd != -1);
                    Debug.Assert(_currentSelectionEnd >= _currentSelectionStart);
                    StringBuilder sb = new StringBuilder();
                    for (var i = _currentSelectionStart; i <= _currentSelectionEnd; ++i)
                    {
                        var selectableElement = _selectableElements[i];
                        if (selectableElement.Type == SelectableElement.ElementType.Text)
                        {
                            sb.Append((string)selectableElement.Contents);
                        }
                    }
                    var dataPackage = new DataPackage();
                    dataPackage.SetText(sb.ToString());
                    Clipboard.SetContent(dataPackage);
                    e.Handled = true;
                }
                else if (e.Key == VirtualKey.A)
                {
                    SelectElements(0, _selectableElements.Count - 1);
                    e.Handled = true;
                }
            }
        }

	    public void ScrollToRegion(DocumentController target)
	    {
		    var offset = target.GetDataDocument().GetDereferencedField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey, null);
		    if (offset == null) return;

		    ScrollViewer.ChangeView(null, offset.Data, null);
	    }
		
		// when the sidebar marker gets pressed
		private void xMarker_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			MarkerSelected((PDFRegionMarker)sender);
			AnnotationManager.SelectRegion(((PDFRegionMarker)sender).LinkTo);
			e.Handled = true;
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
            var docview = new DocumentView()
            {
                DataContext = new DocumentViewModel(note) {DisableDecorations = true},
                Width = xAnnotationBox.ActualWidth,
                BindRenderTransform = false
            };
            docview.hideResizers();

            Canvas.SetTop(docview, region.GetDataDocument().GetField<PointController>(KeyStore.VisualRegionTopLeftPercentileKey).Data.Y * xAnnotations.ActualHeight);
            //SetAnnotationPosition(ScrollViewer.VerticalOffset, docview);
            Annotations.Add(docview);
            DocControllers.Add(docview.ViewModel.LayoutDocument);            //if(AnnotationManager.CurrentAnnotationType.Equals(AnnotationManager.AnnotationType.RegionBox))
            DataDocument.SetField(KeyStore.AnnotationsKey, new ListController<DocumentController>(DocControllers), true);

        }

        private void xAnnotationsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (xAnnotationBox.Visibility.Equals(Visibility.Visible))
            {
               
                xAnnotationBox.Visibility = Visibility.Collapsed;
                xAnnotationBox2.Visibility = Visibility.Collapsed;
            }
            else
            {
                xAnnotationBox.Visibility = Visibility.Visible;
                xAnnotationBox2.Visibility = Visibility.Visible;
            }
        }

        private void XNextPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XPreviousPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void xSplitScreenButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (ScrollViewer2.Visibility.Equals(Visibility.Collapsed))
            {
                ScrollViewer2.Visibility = Visibility.Visible;
                xDivider.Height = new GridLength(6, GridUnitType.Pixel);
                xFirstPanelRow.Height = new GridLength(1, GridUnitType.Star);
                xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                ScrollViewer2.Visibility = Visibility.Collapsed;
                xDivider.Height = new GridLength(0, GridUnitType.Auto);
                xFirstPanelRow.Height = new GridLength(1, GridUnitType.Star);
                xSecondPanelRow.Height = new GridLength(0, GridUnitType.Auto);
            }
        }
    }
}

