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


        private List<SelectableElement> _selectableElements;

        public List<SelectableElement> SelectableElements = new List<SelectableElement>();


        // we store section of selected text in this list of KVPs with the key and value as start and end index, respectively
        private readonly List<KeyValuePair<int, int>> _currentSelections = new List<KeyValuePair<int, int>>();
        public VisualAnnotationManager AnnotationManager { get; }

        public DocumentController LayoutDocument { get; }
        public DocumentController DataDocument { get; }

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

       

        public CustomPdfView(DocumentController document)
        {


            this.InitializeComponent();
            LayoutDocument = document.GetActiveLayout() ?? document;
            DataDocument = document.GetDataDocument();
            _pages1 = new DataVirtualizationSource<ImageSource>(this, ScrollViewer, PageItemsControl);
            _pages2 = new DataVirtualizationSource<ImageSource>(this, ScrollViewer2, PageItemsControl2);
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
                        dmv.DecorationState = false;
                        var docview = new DocumentView();
                        docview.ViewModel = dmv;
                        docview.hideResizers();
                        Annotations.Add(docview);
                    }
                }

			    xBar.Width = xPdfContainer.ActualWidth;
			    
			    

            };
            AnnotationManager = new VisualAnnotationManager(this, LayoutDocument, xAnnotations);
            ScrollViewer2.SizeChanged += (ss, ee) =>
            {
                if (xBar.Width != 0)
                {
                    xBar.Width = ScrollViewer2.ExtentWidth;
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
            
            SelectableElements = strategy.GetSelectableElements(0, pdfDocument.GetNumberOfPages() - 1);

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

        private void SelectElements(int startIndex, int endIndex)
        {
            // if control isn't pressed, reset the selection
            if (!this.IsCtrlPressed())
            {
                if (_currentSelections.Count > 1)
                {
                    _currentSelections.Clear();
                }
            }

            // if there's no current selections or if there's nothing in the list of selections that matches what we're trying to select
            if (!_currentSelections.Any() || !_currentSelections.Any(sel => sel.Key <= startIndex && startIndex <= sel.Value))
            {
                // create a new selection
                _currentSelections.Add(new KeyValuePair<int, int>(-1, -1));
            }
            var currentSelectionStart = _currentSelections.Last().Key;
            var currentSelectionEnd = _currentSelections.Last().Value;

            if (currentSelectionStart == -1)
            {
                for (var i = startIndex; i <= endIndex; ++i)
                {
                    SelectIndex(i);
                }
            }
            else
            {
                for (var i = startIndex; i < currentSelectionStart; ++i)
                {
                    SelectIndex(i);
                }

                for (var i = currentSelectionStart; i < startIndex; ++i)
                {
                    DeselectIndex(i);
                }

                for (var i = currentSelectionEnd + 1; i <= endIndex; ++i)
                {
                    SelectIndex(i);
                }

                for (var i = endIndex + 1; i <= currentSelectionEnd; ++i)
                {
                    DeselectIndex(i);
                }
            }

            // you can't set kvp keys and values, so we have to just create a new one?
            _currentSelections[_currentSelections.Count - 1] = new KeyValuePair<int, int>(startIndex, endIndex);
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
            _currentSelections.Clear();
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
			if (!_currentSelections.Any() || _currentSelections.Last().Key == -1) return;//Not currently selecting anything
			_selectionStartPoint = null;

            // loop through each selection and add the indices in each selection set
		    var indices = new List<int>();
		    foreach (var selection in _currentSelections)
		    {
		        for (var i = selection.Key; i <= selection.Value; i++)
		        {
                    // this will avoid double selecting any items
		            if (!indices.Contains(i))
		            {
		                indices.Add(i);
		            }
		        }
		    }

            // get every matching selectable element and set the selection region to that
		    var selectableElements = new List<SelectableElement>();
		    foreach (var index in indices)
		    {
                selectableElements.Add(SelectableElements[index]);
		    }

            AnnotationManager.SetSelectionRegion(selectableElements);
        }

        private void XPdfGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

            if (AnnotationManager.CurrentAnnotationType.Equals(Dash.AnnotationManager.AnnotationType.TextSelection))
            {
                var mouse = new Point(e.GetPosition(xPdfGrid).X, e.GetPosition(xPdfGrid).Y);
                var closest = GetClosestElementInDirection(mouse, mouse);

                //space, tab, enter

                if ((Math.Abs(closest.Bounds.X - mouse.X) < 10) && Math.Abs(closest.Bounds.Y - mouse.Y) < 10)
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

            e.Handled = true;
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

            if (!this.IsCtrlPressed())
            {
                ClearSelection();
            }
            switch (AnnotationManager.CurrentAnnotationType)
            {
                case Dash.AnnotationManager.AnnotationType.RegionBox:
                    NewRegionStarted?.Invoke(sender, e);
                    break;
                case Dash.AnnotationManager.AnnotationType.TextSelection:
                    e.Handled = true;
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
        private bool _isCtrlPressed;

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
            Pages1.View_SizeChanged(null, null);
            Pages2.View_SizeChanged(null, null);
        }

        private void CustomPdfView_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (this.IsCtrlPressed())
            {
                if (e.Key == VirtualKey.C && _currentSelections.Last().Key != -1)
                {
                    Debug.Assert(_currentSelections.Last().Value != -1);
                    Debug.Assert(_currentSelections.Last().Value >= _currentSelections.Last().Key);
                    StringBuilder sb = new StringBuilder();
                    _currentSelections.Sort((s1, s2) => Math.Sign(s1.Key - s2.Key));

                    // get the indices from our selections and ignore any duplicate selections
                    var indices = new List<int>();
                    foreach (var selection in _currentSelections)
                    {
                        for (var i = selection.Key; i <= selection.Value; i++)
                        {
                            if (!indices.Contains(i))
                            {
                                indices.Add(i);
                            }
                        }
                    }

                    // if there's ever a jump in our indices, insert two line breaks before adding the next index
                    var prevIndex = indices.First();
                    foreach (var index in indices.Skip(1))
                    {
                        if (prevIndex + 1 != index)
                        {
                            sb.Append("\r\n\r\n");
                        }
                        var selectableElement = SelectableElements[index];
                        if (selectableElement.Type == SelectableElement.ElementType.Text)
                        {
                            sb.Append((string)selectableElement.Contents);
                        }

                        prevIndex = index;
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

            Canvas.SetTop(docview, region.GetDataDocument().GetField<PointController>(KeyStore.VisualRegionTopLeftPercentileKey).Data.Y * xAnnotations.ActualHeight);
            //SetAnnotationPosition(ScrollViewer.VerticalOffset, docview);
            Annotations.Add(docview);
            DocControllers.Add(docview.ViewModel.LayoutDocument);            //if(AnnotationManager.CurrentAnnotationType.Equals(AnnotationManager.AnnotationType.RegionBox))
            DataDocument.SetField(KeyStore.AnnotationsKey, new ListController<DocumentController>(DocControllers), true);

        }

        private void XAnnotationsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
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
            ScrollViewer2.ChangeView(null, 0, null);
        }

        private void XNextPageButton2_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MovePage(ScrollViewer2, xAnnotationBox2, 1);

        }

        private void XPreviousPageButton2_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MovePage(ScrollViewer2, xAnnotationBox2, 0);
        }

        private void XScrollBack2_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopStack(_backStack2, ScrollViewer2);
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
            AddToStack(_backStack, ScrollViewer);
            AddToStack(_backStack2, ScrollViewer2);

        }

        private void XNextPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MovePage(ScrollViewer, xAnnotationBox, 1);
        }

        private void XPreviousPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            MovePage(ScrollViewer, xAnnotationBox, 0);
        }

        private void XScrollToTop_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ScrollViewer.ChangeView(null, 0, null);
        }

        private void XScrollBack_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PopStack(_backStack, ScrollViewer);
        }

     
        private void MovePage(ScrollViewer scroller, Grid grid, int i)
        {
            var currOffset = 0.0;
            foreach (var image in PageItemsControl.GetDescendantsOfType<Image>())
            {
                var imgWidth = image.ActualWidth;
                var annoWidth = grid.Visibility == Visibility.Visible ? grid.ActualWidth : 0;
                var scale = (scroller.ViewportWidth - annoWidth) / imgWidth;
                if (i == 0)
                {
                    if (currOffset + (image.ActualHeight * scale) + 5 > scroller.VerticalOffset)
                    {
                        break;
                    }
                    currOffset += (image.ActualHeight * scale);
                }
                else
                {
                    currOffset += (image.ActualHeight * scale);
                    if (currOffset > scroller.VerticalOffset + 5)
                    {
                        break;
                    }
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

        
	    public DocumentView GetDocView()
	    {
		    return this.GetFirstAncestorOfType<DocumentView>();
		}


        
    }
}

