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

        private DataVirtualizationSource<ImageSource> _pages;
        public DataVirtualizationSource<ImageSource> Pages
        {
            get => _pages;
            set
            {
                _pages = value;
                OnPropertyChanged();
            }
        }

        public List<SelectableElement> SelectableElements = new List<SelectableElement>();

        public VisualAnnotationManager AnnotationManager { get; }

        public DocumentController LayoutDocument { get; }
        public DocumentController DataDocument { get; }

        private WPdf.PdfDocument _wPdfDocument;
        public WPdf.PdfDocument PDFdoc => _wPdfDocument;

        public CustomPdfView()
        {
            this.InitializeComponent();
        }

        public CustomPdfView(DocumentController document)
        {
            this.InitializeComponent();
            LayoutDocument = document.GetActiveLayout() ?? document;
            DataDocument = document.GetDataDocument();
            _pages = new DataVirtualizationSource<ImageSource>(this);
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
			            var offset = region.GetDataDocument().GetField<NumberController>(KeyStore.PdfRegionVerticalOffsetKey)
			                .Data;
			            MakeRegionMarker(offset, region);
			        }
			    }
			};

            AnnotationManager = new VisualAnnotationManager(this, LayoutDocument, xAnnotations);
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
            return AnnotationManager?.GetRegionDocument();
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
                Pages.Height = page.GetPageSize().GetHeight();
                Pages.Width = page.GetPageSize().GetWidth();
                maxWidth = Math.Max(maxWidth, page.GetPageSize().GetWidth());
            }

            PdfMaxWidth = maxWidth;

            _wPdfDocument = await WPdf.PdfDocument.LoadFromFileAsync(file);
            await RenderPdf(null);

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

            SelectableElements = strategy.GetSelectableElements(0, pdfDocument.GetNumberOfPages() - 1);

            reader.Close();
            pdfDocument.Close();
            PdfTotalHeight = offset - 10;
            DocumentLoaded?.Invoke(this, new EventArgs());
		}

        public BoundsExtractionStrategy Strategy { get; set; }

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
                //Pages.Clear();
            }

            //for (uint i = 0; i < _wPdfDocument.PageCount; ++i)
            //{
            //    Debug.WriteLine($"{i}/{_wPdfDocument.PageCount}");
            //    if (token.IsCancellationRequested)
            //    {
            //        return;
            //    }
            //    var stream = new InMemoryRandomAccessStream();
            //    var widthRatio = targetWidth == null ? (ActualWidth == 0 ? 1 : (ActualWidth / PdfMaxWidth)) : (targetWidth / PdfMaxWidth);
            //    options.DestinationWidth = (uint)(widthRatio * _wPdfDocument.GetPage(i).Dimensions.MediaBox.Width);
            //    options.DestinationHeight = (uint)(widthRatio * _wPdfDocument.GetPage(i).Dimensions.MediaBox.Height);
            //    await _wPdfDocument.GetPage(i).RenderToStreamAsync(stream, options);
            //    var source = new BitmapImage();
            //    await source.SetSourceAsync(stream);
            //    if (token.IsCancellationRequested)
            //    {
            //        return;
            //    }
            //}
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
            foreach (var selectableElement in SelectableElements)
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
            
            _selectedRectangles[index].Visibility = Visibility.Collapsed;
        }

        private readonly SolidColorBrush _selectionBrush = new SolidColorBrush(Color.FromArgb(120, 0x94, 0xA5, 0xBB));

        private void SelectIndex(int index)
        {
            if (_selectedRectangles.ContainsKey(index))
            {
                _selectedRectangles[index].Visibility = Visibility.Visible;
                if (!TestSelectionCanvas.Children.Contains(_selectedRectangles[index]))
                {
                    TestSelectionCanvas.Children.Add(_selectedRectangles[index]);
                }

                return;
            }

            var elem = SelectableElements[index];
            var rect = new Rectangle
            {
                Width = elem.Bounds.Width,
                Height = elem.Bounds.Height
            };
            Canvas.SetLeft(rect, elem.Bounds.X);
            Canvas.SetTop(rect, elem.Bounds.Y);
            rect.Fill = _selectionBrush;

            _selectedRectangles.Add(index, rect);
            TestSelectionCanvas.Children.Add(rect);
        }


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
            foreach (var rect in _selectedRectangles.Values)
            {
                rect.Visibility = Visibility.Collapsed;
            }
            TestSelectionCanvas.Children.Clear();
			AnnotationManager.SetSelectionRegion(null);
        }

        private void EndSelection()
		{
			if (_currentSelectionStart == -1) return;//Not currently selecting anything
			_selectionStartPoint = null;
            AnnotationManager.SetSelectionRegion(SelectableElements.Skip(_currentSelectionStart).Take(_currentSelectionEnd - _currentSelectionStart + 1));
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
                    var selectableElement = SelectableElements[i];
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
                    var selectableElement = SelectableElements[i];
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
        private double _height;
        private double _width;
        private double _verticalOffset;
        public void CustomPdfView_OnSizeChanged(object sender, SizeChangedEventArgs e)
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
            //await RenderPdf(ScrollViewer.ActualWidth);
            Pages.View_SizeChanged(null, null);
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
                        var selectableElement = SelectableElements[i];
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
                    SelectElements(0, SelectableElements.Count - 1);
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
		
	}
}

