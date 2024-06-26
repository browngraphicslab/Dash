﻿using Dash.Annotations;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
using Point = Windows.Foundation.Point;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using WPdf = Windows.Data.Pdf;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
	public sealed partial class CustomPdfView : UserControl, INotifyPropertyChanged, ILinkHandler
	{
		public static readonly DependencyProperty PdfUriProperty = DependencyProperty.Register(
			"PdfUri", typeof(Uri), typeof(CustomPdfView), new PropertyMetadata(default(Uri), PropertyChangedCallback));

		private List<PDFRegionMarker> _markers = new List<PDFRegionMarker>();

		public Uri PdfUri
		{
			get => (Uri) GetValue(PdfUriProperty);
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

		private DataVirtualizationSource<ImageSource> _topPages;

		public DataVirtualizationSource<ImageSource> TopPages
		{
			get => _topPages;
			set
			{
				_topPages = value;
				OnPropertyChanged();
			}
		}

		private DataVirtualizationSource<ImageSource> _bottomPages;

		public DataVirtualizationSource<ImageSource> BottomPages
		{
			get => _bottomPages;
			set
			{
				_bottomPages = value;
				OnPropertyChanged();
			}
		}

		private ObservableCollection<DocumentView> _topAnnotationList = new ObservableCollection<DocumentView>();

		public ObservableCollection<DocumentView> TopAnnotations
		{
			get => _topAnnotationList;
			set
			{
				_topAnnotationList = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<DocumentView> BottomAnnotations
		{
			get => _bottomAnnotationList;
			set
			{
				_bottomAnnotationList = value;
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



		public DocumentController LayoutDocument { get; }
		public DocumentController DataDocument { get; }

		//This makes the assumption that both pdf views are always in the same annotation mode
		public AnnotationType CurrentAnnotationType => _bottomAnnotationOverlay.AnnotationType;

		private WPdf.PdfDocument _wPdfDocument;
		private PDFRegionMarker _currentMarker;

		private Stack<double> _topBackStack;
		private Stack<double> _bottomBackStack;

		private Stack<double> _topForwardStack;
		private Stack<double> _bottomForwardStack;

		private DispatcherTimer _topTimer;
		private DispatcherTimer _bottomTimer;

		public Grid TopAnnotationBox => xTopAnnotationBox;
		public Grid BottomAnnotationBox => xBottomAnnotationBox;


		public WPdf.PdfDocument PDFdoc => _wPdfDocument;

		private void CustomPdfView_Loaded(object sender, RoutedEventArgs routedEventArgs)
		{
			LayoutDocument.AddFieldUpdatedListener(KeyStore.GoToRegionKey, GoToUpdated);
            this.KeyDown += CustomPdfView_KeyDown;
			_bottomAnnotationOverlay.LoadPinAnnotations(this);
			_topAnnotationOverlay.LoadPinAnnotations(this);
		}

        class SelRange {
            public KeyValuePair<int, int> Range;
            public Rect ClipRect;
        }

        private void CustomPdfView_KeyDown(object sender, KeyRoutedEventArgs args)
        {

            if (args.Key == VirtualKey.Space)
                MainPage.Instance.xToolbar.xPdfToolbar.Update(
                    CurrentAnnotationType == AnnotationType.Region ? AnnotationType.Selection : AnnotationType.Region);
            if (this.IsCtrlPressed())
            {
                var selections = new List<List<SelRange>>
                {
                    _bottomAnnotationOverlay._currentSelections.Zip(_bottomAnnotationOverlay._currentSelectionClipRects, (map, clip) => new SelRange() { Range = map, ClipRect = clip }).ToList(),
                    _topAnnotationOverlay._currentSelections.Zip(_bottomAnnotationOverlay._currentSelectionClipRects, (map, clip) => new SelRange() { Range = map, ClipRect = clip }).ToList(),
                };
                var allSelections = selections.SelectMany(s => s.ToList()).ToList();
                if (args.Key == VirtualKey.C && allSelections.Count > 0 && allSelections.Last().Range.Key != -1)
                {
                    Debug.Assert(allSelections.Last().Range.Value != -1);
                    Debug.Assert(allSelections.Last().Range.Value >= allSelections.Last().Range.Key);
                    StringBuilder sb = new StringBuilder();
                    allSelections.Sort((s1, s2) => Math.Sign(s1.Range.Key - s2.Range.Key));

                    // get the indices from our selections and ignore any duplicate selections
                    var indices = new List<int>();
                    foreach (var selection in allSelections)
                    {
                        for (var i = selection.Range.Key; i <= selection.Range.Value; i++)
                        {
                            if (!indices.Contains(i))
                            {
                                var eleBounds = _bottomAnnotationOverlay._textSelectableElements[i].Bounds;
                                if (selection.ClipRect == null || selection.ClipRect == Rect.Empty ||  selection.ClipRect.Contains(new Point(eleBounds.X+eleBounds.Width/2, eleBounds.Y + eleBounds.Height/2)))
                                    indices.Add(i);
                            }
                        }
                    }

                    // if there's ever a jump in our indices, insert two line breaks before adding the next index
                    var prevIndex = indices.First()-1;
                    foreach (var index in indices)
                    {
                        if (prevIndex + 1 != index)
                        {
                            sb.Append("\r\n\r\n");
                        }
                        var selectableElement = _bottomAnnotationOverlay._textSelectableElements[index];
                        var nchar = ((string)selectableElement.Contents).First();
                        if (prevIndex > 0 && sb.Length > 0 && (nchar > 128 || char.IsUpper(nchar) || (!char.IsWhiteSpace(sb[sb.Length - 1]) && !char.IsPunctuation(sb[sb.Length-1]) && !char.IsLower(sb[sb.Length-1]))) && _bottomAnnotationOverlay._textSelectableElements[prevIndex].Bounds.Bottom < _bottomAnnotationOverlay._textSelectableElements[index].Bounds.Top)
                            sb.Append("\r\n\r\n");
                        if (selectableElement.Type == SelectableElement.ElementType.Text)
                        {
                            sb.Append((string)selectableElement.Contents);
                        }

						prevIndex = index;
					}

                    var dataPackage = new DataPackage();
                    dataPackage.SetText(sb.ToString());
                    dataPackage.Properties[nameof(DocumentController)] = this.LayoutDocument;
                    Clipboard.SetContent(dataPackage);
                    args.Handled = true;
                }
            }
        }

		private void GoToUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args,
			Context context)
		{
			if (args.NewValue == null)
			{
				return;
			}

			ScrollToRegion(args.NewValue as DocumentController);
			_bottomAnnotationOverlay.SelectRegion(args.NewValue as DocumentController);

			sender.RemoveField(KeyStore.GoToRegionKey);
		}

		private void CustomPdfView_Unloaded(object sender, RoutedEventArgs e)
		{
			LayoutDocument.RemoveFieldUpdatedListener(KeyStore.GoToRegionKey, GoToUpdated);
			_bottomAnnotationOverlay._textSelectableElements?.Clear();
			_topAnnotationOverlay._textSelectableElements?.Clear();
		}

		private readonly NewAnnotationOverlay _topAnnotationOverlay;
		private readonly NewAnnotationOverlay _bottomAnnotationOverlay;



		public CustomPdfView(DocumentController document)
		{
			this.InitializeComponent();
			SetUpToolTips();
			LayoutDocument = document.GetActiveLayout() ?? document;
			DataDocument = document.GetDataDocument();
			_topPages = new DataVirtualizationSource<ImageSource>(this, TopScrollViewer, TopPageItemsControl);
			_bottomPages = new DataVirtualizationSource<ImageSource>(this, BottomScrollViewer, BottomPageItemsControl);
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
			Loaded += CustomPdfView_Loaded;
			Unloaded += CustomPdfView_Unloaded;
			SelectionManager.SelectionChanged += SelectionManagerOnSelectionChanged;

			_bottomAnnotationOverlay =
				new NewAnnotationOverlay(LayoutDocument, RegionGetter)
				{
					DataContext = new NewAnnotationOverlayViewModel()
				};
			_topAnnotationOverlay =
				new NewAnnotationOverlay(LayoutDocument, RegionGetter)
				{
					DataContext = new NewAnnotationOverlayViewModel()
				};
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

			_topBackStack = new Stack<double>();
			_topBackStack.Push(0);
			_bottomBackStack = new Stack<double>();
			_bottomBackStack.Push(0);
			_topForwardStack = new Stack<double>();
			_bottomForwardStack = new Stack<double>();

			BottomScrollViewer.ViewChanged += ScrollViewer_ViewChanged;
			TopScrollViewer.ViewChanged += ScrollViewer_ViewChanged;

			Canvas.SetZIndex(xBottomButtonPanel, 999);
			Canvas.SetZIndex(xTopButtonPanel, 999);

			_topTimer = new DispatcherTimer()
			{
				Interval = new TimeSpan(0, 0, 0, 0, 500)
			};
			_topTimer.Tick += TimerTick;

			_bottomTimer = new DispatcherTimer()
			{
				Interval = new TimeSpan(0, 0, 0, 0, 500)
			};
			_bottomTimer.Tick += TimerTick;

			_topTimer.Start();
			_bottomTimer.Start();

			SetAnnotationType(AnnotationType.Region);
		}

		private void SelectionManagerOnSelectionChanged(DocumentSelectionChangedEventArgs args)
		{
			var docview = this.GetFirstAncestorOfType<DocumentView>();
			if (SelectionManager.IsSelected(docview))
			{
				ShowPdfControls();
			}
			else
			{
				HidePdfControls();
			}
		}

		private void TimerTick(object sender, object o)
		{

			if (sender.Equals(_topTimer))
			{
				_topTimer.Stop();
				AddToStack(_topBackStack, TopScrollViewer);
				//_topTimer.Start();

			}

			else if (sender.Equals(_bottomTimer))
			{
				_bottomTimer.Stop();
				AddToStack(_bottomBackStack, BottomScrollViewer);
				//_bottomTimer.Start();
			}
		}



		private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			if (sender.Equals(TopScrollViewer))
			{

				if (TopScrollViewer.ExtentHeight != 0)
				{
					_topTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
					_topTimer.Start();
				}

			}

			else if (sender.Equals(BottomScrollViewer))
			{
				if (BottomScrollViewer.ExtentHeight != 0)
				{
					_bottomTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
					_bottomTimer.Start();
				}

				//check if annotations have left the screen
				foreach (FrameworkElement child in _bottomAnnotationOverlay.XAnnotationCanvas.Children)
				{
					//get linked annotations
					var docController = (child.DataContext as NewAnnotationOverlay.SelectionViewModel)?.RegionDocument;
					bool scrollVisMode = docController?.Tag?.Equals(PinAnnotationVisibility.VisibleOnScroll) ?? false;
					if (scrollVisMode)
					{
						var toLinks = docController?.GetDataDocument().GetLinks(KeyStore.LinkToKey)?.TypedData;
						var fromLinks = docController?.GetDataDocument().GetLinks(KeyStore.LinkFromKey)?.TypedData;

						//bool for checking whether child is currently in view of scrollviewer
						bool inView =
							new Rect(0, 0, BottomScrollViewer.ActualWidth, BottomScrollViewer.ActualHeight).Contains(
								child.TransformToVisual(BottomScrollViewer).TransformPoint(new Point(0, 0)));

						if (toLinks != null)
						{
							foreach (var link in toLinks)
							{
								var sourceDoc = link.GetDataDocument()
									.GetField<DocumentController>(KeyStore.LinkDestinationKey, true);
								//if ui element is currently visible, show annotations
								bool userCreated =
									sourceDoc?.GetField<TextController>(KeyStore.LinkContextKey, true)?.Data ==
									nameof(LinkContexts.PushPin);
								sourceDoc?.SetHidden(!inView);
							}
						}

						if (fromLinks != null)
						{
							foreach (var link in fromLinks)
							{
								var sourceDoc = link.GetDataDocument()
									.GetField<DocumentController>(KeyStore.LinkSourceKey, true);
								if (sourceDoc?.GetField<TextController>(KeyStore.LinkContextKey, true)?.Data !=
								    nameof(LinkContexts.PushPin)) sourceDoc?.SetHidden(!inView);
							}
						}
					}
				}


			}

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

		public DocumentController GetRegionDocument(Point? point = null)
		{
			if (point == null)
				return _bottomAnnotationOverlay.GetRegionDoc() ?? LayoutDocument;

			//if point !null & region is selected, return region 
			var regionDoc = _bottomAnnotationOverlay.GetRegionDoc();

			if (regionDoc != null)
				return regionDoc;

			//else, make a new push pin region closest to given point
			var newPoint = calculateClosestPointOnPDF(point ?? new Point());

            var makeAnnotationPinDoc = _bottomAnnotationOverlay.MakeAnnotationPinDoc(newPoint);
            return makeAnnotationPinDoc;
		}

		private Point calculateClosestPointOnPDF(Point from)
		{
            var p = Util.PointTransformFromVisual(from, MainPage.Instance, this._bottomAnnotationOverlay);
            return  new Point(p.X < 0 ? 30 : p.X > this._bottomAnnotationOverlay.ActualWidth ? this._bottomAnnotationOverlay.ActualWidth - 30 : p.X,
                                    p.Y < 0 ? 30 : p.Y > this._bottomAnnotationOverlay.ActualHeight ? this._bottomAnnotationOverlay.ActualHeight - 30 : p.Y);
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

			;
			var reader = new PdfReader(await file.OpenStreamForReadAsync());
			var pdfDocument = new PdfDocument(reader);
			var strategy = new BoundsExtractionStrategy();
			var processor = new PdfCanvasProcessor(strategy);
			double offset = 0;
			double maxWidth = 0;
			for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
			{
				var page = pdfDocument.GetPage(i);
				TopPages.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
				BottomPages.PageSizes.Add(new Size(page.GetPageSize().GetWidth(), page.GetPageSize().GetHeight()));
				maxWidth = Math.Max(maxWidth, page.GetPageSize().GetWidth());
			}

			PdfMaxWidth = maxWidth;

			_wPdfDocument = await WPdf.PdfDocument.LoadFromFileAsync(file);
			bool add = _wPdfDocument.PageCount != _currentPageCount;
			if (add)
			{
				_currentPageCount = (int) _wPdfDocument.PageCount;
			}

			iText.Kernel.Geom.Rectangle pageSize = null;
			await Task.Run(() => {
                for (var i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
                {
                    var page = pdfDocument.GetPage(i);
                    var size = page.GetPageSize();
                    strategy.SetPage(i - 1, offset, size, page.GetRotation());
                    offset += page.GetPageSize().GetHeight() + 10;
                    processor.ProcessPageContent(page);
					pageSize = pdfDocument.GetPage(1).GetPageSize();
				}
            });
            
            var selectableElements = strategy.GetSelectableElements(0, pdfDocument.GetNumberOfPages());
            _topAnnotationOverlay.SetSelectableElements(selectableElements.Item1);
            _bottomAnnotationOverlay.SetSelectableElements(selectableElements.Item1);

            DataDocument.SetField<TextController>(KeyStore.DocumentTextKey, selectableElements.Item2, true);

            reader.Close();
            pdfDocument.Close();
            PdfTotalHeight = offset - 10;
			DataDocument.SetField(KeyStore.PdfHeightKey, new PointController(pageSize.GetWidth(), pageSize.GetHeight()), true);
            DocumentLoaded?.Invoke(this, new EventArgs());

			_bottomAnnotationOverlay.LoadPinAnnotations(this);
			_topAnnotationOverlay.LoadPinAnnotations(this);
			MainPage.Instance.ClosePopup();
		}

		public BoundsExtractionStrategy Strategy { get; set; }

		private int _currentPageCount = -1;

		private static async void PropertyChangedCallback(DependencyObject dependencyObject,
			DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
		{
			await ((CustomPdfView) dependencyObject).OnPdfUriChanged();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
        
		private void XPdfGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (CurrentAnnotationType == AnnotationType.Region)
			{
				var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
                overlay.StartAnnotation(e.GetPosition(overlay), true);
			}

		}

		#region Region/Selection Events

		private void XPdfGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            (sender as FrameworkElement).PointerMoved -= XPdfGrid_PointerMoved;
            var currentPoint = e.GetCurrentPoint(TopPageItemsControl);
			if (currentPoint.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased)
			{
				return;
			}

			var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
			overlay.EndAnnotation(e.GetCurrentPoint(overlay).Position);
			e.Handled = true;
			var curPt = e.GetCurrentPoint(this).Position;
			var delta = new Point(curPt.X - _downPt.X, curPt.Y - _downPt.Y);
			var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
			if (!SelectionManager.IsSelected(this.GetFirstAncestorOfType<DocumentView>()) && dist > 10)
			{
				SelectionManager.Select(this.GetFirstAncestorOfType<DocumentView>(), this.IsShiftPressed());
			}
            this.Focus(FocusState.Pointer);
		}

		private void XPdfGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			var currentPoint = e.GetCurrentPoint(TopPageItemsControl);
			var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
			if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
			{
				overlay.EndAnnotation(e.GetCurrentPoint(overlay).Position);
				return;
			}

            if (currentPoint.Properties.IsLeftButtonPressed)
            {
                overlay.UpdateAnnotation(e.GetCurrentPoint(overlay).Position);
            }

			//e.Handled = true;
		}

		Point _downPt = new Point();

		private void XPdfGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_downPt = e.GetCurrentPoint(this).Position;
			var currentPoint = e.GetCurrentPoint(TopPageItemsControl);
			var overlay = sender == xTopPdfGrid ? _topAnnotationOverlay : _bottomAnnotationOverlay;
            if (currentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                overlay.StartAnnotation(e.GetCurrentPoint(overlay).Position);
                (sender as FrameworkElement).PointerMoved -= XPdfGrid_PointerMoved;
                (sender as FrameworkElement).PointerMoved += XPdfGrid_PointerMoved;
            }
        }



		#endregion

		// ScrollViewers don't deal well with being resized so we have to manually track the scroll ratio and restore it on SizeChanged
		private double _topScrollRatio;
		private double _bottomScrollRatio;
		private double _height;
		private double _width;
		private double _verticalOffset;
		private bool _isCtrlPressed;
		private ObservableCollection<DocumentView> _bottomAnnotationList = new ObservableCollection<DocumentView>();

		public void CustomPdfView_OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			TopScrollViewer.ChangeView(null, _topScrollRatio * TopScrollViewer.ExtentHeight, null, true);
			BottomScrollViewer.ChangeView(null, _bottomScrollRatio * BottomScrollViewer.ExtentHeight, null, true);
		}

		private void ScrollViewer_OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
		{
			if (sender.Equals(TopScrollViewer))
			{
				_topScrollRatio = e.FinalView.VerticalOffset / TopScrollViewer.ExtentHeight;
			}
			else if (sender.Equals(BottomScrollViewer))
			{
				_bottomScrollRatio = e.FinalView.VerticalOffset / BottomScrollViewer.ExtentHeight;
			}


			//LayoutDocument.SetField<NumberController>(KeyStore.PdfVOffsetFieldKey, _topScrollRatio, true);
		}

		public void UnFreeze()
		{
			//await RenderPdf(ScrollViewer.ActualWidth);
			TopPages.View_SizeChanged();
			BottomPages.View_SizeChanged();
		}

		public void ScrollToPosition(double pos)
		{
			var sizes = _bottomPages.PageSizes;
			var botOffset = 0.0;
			var annoWidth = xBottomAnnotationBox.ActualWidth;
			foreach (var size in sizes)
			{
				var scale = (BottomScrollViewer.ViewportWidth - annoWidth) / size.Width;
				if (botOffset + (size.Height * scale) - pos > 1)
				{
					break;
				}

				botOffset += (size.Height * scale) + 15;
			}

			xFirstPanelRow.Height = new GridLength(0, GridUnitType.Star);
			xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
			BottomScrollViewer.ChangeView(null, botOffset, null);
		}

        public void ScrollToRegion(DocumentController target, DocumentController source = null)
        {
            var ratioOffsets = target.GetField<ListController<NumberController>>(KeyStore.PDFSubregionKey);
            if (ratioOffsets == null) return;

            var offsets = ratioOffsets.TypedData.Select(i => i.Data * TopScrollViewer.ExtentHeight).ToList();

            var currOffset = offsets.First();
            var firstOffset = offsets.First();
            var maxOffset = BottomScrollViewer.ViewportHeight;
            var splits = new List<double>();
            

            if (source != null)
            {
                currOffset = 0;
                foreach (var offset in offsets)
                {
                    if (currOffset == 0 || offset - currOffset > maxOffset)
                    {
                        splits.Add(offset);
                        currOffset = offset;
                    }
                }

                var off = source.GetField<ListController<NumberController>>(KeyStore.PDFSubregionKey)[0].Data * BottomScrollViewer.ExtentHeight;
                splits.Insert(1, off);
                offsets.Insert(1, off);
            }
            else
            {
                foreach (var offset in offsets.Skip(1))
                {
                    if (offset - currOffset > maxOffset)
                    {
                        splits.Add(offset);
                        currOffset = offset;
                    }
                }
            }
            
            Debug.WriteLine($"{splits} screen splits are needed to show everything");

			var sizes = _bottomPages.PageSizes;
			// TODO: functionality for more than one split maybe?
			if (splits.Any())
			{
				var botOffset = 0.0;
				var annoWidth = xBottomAnnotationBox.ActualWidth;
				foreach (var size in sizes)
				{
					var scale = (BottomScrollViewer.ViewportWidth - annoWidth) / size.Width;

					if (botOffset + (size.Height * scale) + 15 - splits[0] >= -1)

					{
						break;
					}

					botOffset += (size.Height * scale) + 15;
				}

				var topOffset = 0.0;
				annoWidth = xTopAnnotationBox.ActualWidth;
				foreach (var size in sizes)
				{
					var scale = (TopScrollViewer.ViewportWidth - annoWidth) / size.Width;

					if (topOffset + (size.Height * scale) + 15 - firstOffset >= -1)
					{
						break;
					}

					topOffset += size.Height * scale + 15;
				}

                xFirstPanelRow.Height = new GridLength(1, GridUnitType.Star);
                xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
                TopScrollViewer.ChangeView(null,
                    offsets.First() - (BottomScrollViewer.ViewportHeight + TopScrollViewer.ViewportHeight) / 4, null);
                BottomScrollViewer.ChangeView(null,
                    offsets.Skip(1).First() - (BottomScrollViewer.ViewportHeight + TopScrollViewer.ViewportHeight) / 4,
                    null, true);
            }
            else
            {
                var annoWidth = xBottomAnnotationBox.ActualWidth;
                var botOffset = 0.0;
                foreach (var size in sizes)
                {
                    var scale = (BottomScrollViewer.ViewportWidth - annoWidth) / size.Width;

					if (botOffset + (size.Height * scale) + 15 - firstOffset >= -1)

					{
						break;
					}

					botOffset += (size.Height * scale) + 15;
				}

				xFirstPanelRow.Height = new GridLength(0, GridUnitType.Star);
				xSecondPanelRow.Height = new GridLength(1, GridUnitType.Star);
				BottomScrollViewer.ChangeView(null,
					offsets.First() - (TopScrollViewer.ViewportHeight + BottomScrollViewer.ViewportHeight) / 2, null);
			}
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
				if (region.Offset > currOffset && Math.Abs(region.Offset - currOffset) > 1 &&
				    (nextOffset == null || region.Offset < nextOffset.Offset))
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
				if (region.Offset < currOffset && Math.Abs(region.Offset - currOffset) > 1 &&
				    (prevOffset == null || region.Offset > prevOffset.Offset))
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

			var region = (sender == xTopAnnotationBox ? _topAnnotationOverlay : _bottomAnnotationOverlay)
				.GetRegionDoc();
			if (region == null)
			{
				var yPos = e.GetPosition(sender as UIElement).Y;
				region = RegionGetter(AnnotationType.Region);
				region.SetPosition(new Point(0, yPos));
				region.SetWidth(50);
				region.SetHeight(20);
				(sender == xTopAnnotationBox ? _topAnnotationOverlay : _bottomAnnotationOverlay)
					.RenderNewRegion(region);

				// note is the new annotation textbox that is created
				var note = new RichTextNote("<annotation>", new Point(0, region.GetPosition()?.Y ?? 0),
					new Size(xTopAnnotationBox.Width / 2, double.NaN)).Document;

				region.Link(note, LinkContexts.None);
				var docview = new DocumentView
				{
					DataContext = new DocumentViewModel(note) {Undecorated = true},
					Width = xTopAnnotationBox.ActualWidth,
					BindRenderTransform = false,
					RenderTransform = new TranslateTransform
					{
						Y = yPos
					}
				};
				docview.hideResizers();

				//SetAnnotationPosition(ScrollViewer.VerticalOffset, docview);
				BottomAnnotations.Add(docview);
				DocControllers.Add(docview.ViewModel
					.LayoutDocument); //if(AnnotationManager.CurrentAnnotationType.Equals(AnnotationManager.AnnotationType.RegionBox))
				DataDocument.SetField(KeyStore.AnnotationsKey, new ListController<DocumentController>(DocControllers),
					true);
			}
			else
			{
				// note is the new annotation textbox that is created
				var note = new RichTextNote("<annotation>", new Point(), new Size(xTopAnnotationBox.Width, double.NaN))
					.Document;

				region.Link(note, LinkContexts.None);
				var docview = new DocumentView
				{
					DataContext = new DocumentViewModel(note) {DecorationState = false},
					Width = xTopAnnotationBox.ActualWidth,
					BindRenderTransform = false
				};
				docview.hideResizers();

				Canvas.SetTop(docview, region.GetDataDocument().GetPosition()?.Y ?? 0);
				//SetAnnotationPosition(ScrollViewer.VerticalOffset, docview);
				BottomAnnotations.Add(docview);
				DocControllers.Add(docview.ViewModel
					.LayoutDocument); //if(AnnotationManager.CurrentAnnotationType.Equals(AnnotationManager.AnnotationType.RegionBox))
				DataDocument.SetField(KeyStore.AnnotationsKey, new ListController<DocumentController>(DocControllers),
					true);
			}

			SelectionManager.Select(this.GetFirstAncestorOfType<DocumentView>(), false);
		}

		private void XTopAnnotationsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (xTopAnnotationBox.Width.Equals(0))
			{
				xTopAnnotationBox.Width = 200;
				xBottomAnnotationBox.Width = 200;

			}
			else
			{
				xTopAnnotationBox.Width = 0;
				xBottomAnnotationBox.Width = 0;
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

		private void XBottomScrollToTop_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			BottomScrollViewer.ChangeView(null, 0, null);
			_bottomBackStack.Push(0);
		}

		private void XBottomNextPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			PageNext(BottomScrollViewer);

		}

		private void XBottomPreviousPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			PagePrev(BottomScrollViewer);
		}

		private void XBottomScrollBack_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			PopBackStack(_bottomBackStack, _bottomForwardStack, BottomScrollViewer);
		}

		private void XTopNextPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			PageNext(TopScrollViewer);
		}

		private void XTopPreviousPageButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			PagePrev(TopScrollViewer);
		}

		private void XTopScrollToTop_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			TopScrollViewer.ChangeView(null, 0, null);
			_topBackStack.Push(0);
		}

		private void XTopScrollBack_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			PopBackStack(_topBackStack, _topForwardStack, TopScrollViewer);
		}


		private void PagePrev(ScrollViewer scroller)
		{
			DataVirtualizationSource<ImageSource> pages;
			double annoWidth = 0;
			if (scroller.Equals(TopScrollViewer))
			{

				pages = _topPages;
				annoWidth = xTopAnnotationBox.Width;

			}

			else
			{
				pages = _bottomPages;
				annoWidth = xBottomAnnotationBox.Width;

			}

			var sizes = pages.PageSizes;
			var currOffset = 0.0;
			foreach (var size in sizes)
			{
				var scale = (scroller.ViewportWidth - annoWidth) / size.Width;

				if (currOffset + (size.Height + 10) * scale - scroller.VerticalOffset >= -1)

				{
					break;
				}

				currOffset += (size.Height + 10) * scale;
			}

			scroller.ChangeView(null, currOffset, 1);
			if (scroller.Equals(TopScrollViewer))
			{
				_topBackStack.Push(currOffset / TopScrollViewer.ExtentHeight);
			}
			else if (scroller.Equals(BottomScrollViewer))
			{
				_bottomBackStack.Push(currOffset / BottomScrollViewer.ExtentHeight);
			}

		}

		private void PageNext(ScrollViewer scroller)
		{
			DataVirtualizationSource<ImageSource> pages;
			double annoWidth;
			if (scroller.Equals(TopScrollViewer))
			{

				pages = _topPages;
				annoWidth = xTopAnnotationBox.Width;

			}

			else
			{
				pages = _bottomPages;
				annoWidth = xBottomAnnotationBox.Width;

			}

			var sizes = pages.PageSizes;
			var currOffset = 0.0;
			foreach (var size in sizes)
			{
				var scale = (scroller.ViewportWidth - annoWidth) / size.Width;
				currOffset += (size.Height + 10) * scale;
				if (currOffset - scroller.VerticalOffset > 1)
				{
					break;
				}

			}

			scroller.ChangeView(null, currOffset, 1);
			if (scroller.Equals(TopScrollViewer))
			{
				_topBackStack.Push(currOffset / TopScrollViewer.ExtentHeight);
			}
			else if (scroller.Equals(BottomScrollViewer))
			{
				_bottomBackStack.Push(currOffset / BottomScrollViewer.ExtentHeight);
			}

		}

		public void GoToPage(double pageNum)
		{

			var sizes = BottomPages.PageSizes;
			var currOffset = 0.0;

			for (var i = 0; i < pageNum - 1; i++)
			{
				var scale = (BottomScrollViewer.ViewportWidth - xBottomAnnotationBox.Width) / sizes[i].Width;
				currOffset += (sizes[i].Height + 10) * scale;
			}

			BottomScrollViewer.ChangeView(null, currOffset, 1);
			TopScrollViewer.ChangeView(null, currOffset, 1);

		}

		private void AddToStack(Stack<double> stack, ScrollViewer viewer)
		{
			if (!stack.Count().Equals(0))
			{
				if (!stack.Peek().Equals(viewer.VerticalOffset / viewer.ExtentHeight))
				{
					stack.Push(viewer.VerticalOffset / viewer.ExtentHeight);
				}
			}
			else
			{
				stack.Push(0);
				stack.Push(viewer.VerticalOffset / viewer.ExtentHeight);
			}
		}

		private void PopBackStack(Stack<double> backstack, Stack<double> forwardstack, ScrollViewer viewer)
		{
			if (backstack.Any())
			{
				var pop = backstack.Pop();
				if (backstack.Count > 0 && !backstack.Peek().Equals(Double.NaN) &&
				    !viewer.ExtentHeight.Equals(Double.NaN))
				{
					viewer.ChangeView(null, backstack.Peek() * viewer.ExtentHeight, 1);
				}
				else
				{
					viewer.ChangeView(null, 0, 1);
				}

				//viewer.ChangeView(null, backstack.Any() ? backstack.Peek() * viewer.ExtentHeight : 0, 1);
				forwardstack.Push(pop);
			}
		}

		private void XBottomControlsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
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

		private void XTopControlsToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (xTopToggleButtonStack.Visibility.Equals(Visibility.Collapsed))
			{
				xTopToggleButtonStack.Visibility = Visibility.Visible;
				xFadeAnimation.Begin();
			}
			else
			{
				xTopToggleButtonStack.Visibility = Visibility.Collapsed;
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

        public LinkHandledResult HandleLink(DocumentController linkDoc, LinkDirection direction)
        {
            if (_bottomAnnotationOverlay.RegionDocsList.Contains(linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey)))
            {
                var src = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey);
                ScrollToRegion(src);
            }
            var target = linkDoc.GetLinkedDocument(direction);
            if (_bottomAnnotationOverlay.RegionDocsList.Contains(target))
            {
                ScrollToRegion(target, linkDoc.GetLinkedDocument(direction, true));
                return LinkHandledResult.HandledClose;
            }

			return LinkHandledResult.Unhandled;
		}

		private void XTopScrollForward_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			PopForwardStack(_topForwardStack, TopScrollViewer);
		}

		private void XBottomScrollForward_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			PopForwardStack(_bottomForwardStack, BottomScrollViewer);
		}

		private void PopForwardStack(Stack<double> forwardstack, ScrollViewer viewer)
		{
			if (forwardstack.Any())
			{
				var pop = forwardstack.Pop();
				viewer.ChangeView(null, forwardstack.Any() ? forwardstack.Peek() * viewer.ExtentHeight : 0, 1);
			}
		}

		private void Test(object sender, KeyRoutedEventArgs e)
		{

		}



		public void HidePdfControls()
		{
			xTopButtonPanel.Visibility = Visibility.Collapsed;
			xBottomButtonPanel.Visibility = Visibility.Collapsed;
		}

		public void ShowPdfControls()
		{
			xTopButtonPanel.Visibility = Visibility.Visible;
			xBottomButtonPanel.Visibility = Visibility.Visible;

			xFadeAnimation.Begin();
			xFadeAnimation2.Begin();
		}

		private ToolTip _controlsTop;
		private ToolTip _controlsBottom;

		private ToolTip _nextTop;
		private ToolTip _nextBottom;

		private ToolTip _prevTop;
		private ToolTip _prevBottom;

		private ToolTip _upTop;
		private ToolTip _upBottom;

		private ToolTip _backTop;
		private ToolTip _backBottom;

		private ToolTip _forwardTop;
		private ToolTip _forwardBottom;

		private void SetUpToolTips()
		{
			var placementMode = PlacementMode.Bottom;
			const int offset = 0;

			_controlsTop = new ToolTip()
			{
				Content = "Toggle controls",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xTopAnnotationsToggleButton, _controlsTop);

			_controlsBottom = new ToolTip()
			{
				Content = "Toggle controls",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xBottomAnnotationsToggleButton, _controlsBottom);

			_nextTop = new ToolTip()
			{
				Content = "Next page",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xTopNextPageButton, _nextTop);

			_nextBottom = new ToolTip()
			{
				Content = "Next page",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xBottomNextPageButton, _nextBottom);

			_prevTop = new ToolTip()
			{
				Content = "Previous page",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xTopPreviousPageButton, _prevTop);

			_prevBottom = new ToolTip()
			{
				Content = "Previous page",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xBottomPreviousPageButton, _prevBottom);

			_upTop = new ToolTip()
			{
				Content = "Scroll to top",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xTopScrollToTop, _upTop);

			_upBottom = new ToolTip()
			{
				Content = "Scroll to top",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xBottomScrollToTop, _upBottom);

			_backTop = new ToolTip()
			{
				Content = "Scroll backward",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xTopScrollBack, _backTop);

			_backBottom = new ToolTip()
			{
				Content = "Scroll backward",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xBottomScrollBack, _backBottom);

			_forwardTop = new ToolTip()
			{
				Content = "Scroll forward",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xTopScrollForward, _forwardTop);

			_forwardBottom = new ToolTip()
			{
				Content = "Scroll forward",
				Placement = placementMode,
				VerticalOffset = offset
			};
			ToolTipService.SetToolTip(xBottomScrollForward, _forwardBottom);



		}

		private void XOnPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor =
				new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
			if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
		}

		private void XOnPointerExited(object sender, PointerRoutedEventArgs e)
		{
			Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor =
				new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
			if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
		}

		public void SetAnnotationsVisibleOnScroll(bool enabled)
		{
			foreach (FrameworkElement child in _bottomAnnotationOverlay.XAnnotationCanvas.Children)
			{
				//get linked annotations
				var docController = (child.DataContext as NewAnnotationOverlay.SelectionViewModel)?.RegionDocument;
				if (docController != null)
					docController.Tag = enabled
						? PinAnnotationVisibility.VisibleOnScroll
						: PinAnnotationVisibility.ManualToggle;
			}

			foreach (FrameworkElement child in _topAnnotationOverlay.XAnnotationCanvas.Children)
			{
				//get linked annotations
				var docController = (child.DataContext as NewAnnotationOverlay.SelectionViewModel)?.RegionDocument;
				if (docController != null)
					docController.Tag = enabled
						? PinAnnotationVisibility.VisibleOnScroll
						: PinAnnotationVisibility.ManualToggle;
			}

		}

		private void xPdfDivider_Tapped(object sender, TappedRoutedEventArgs e)
		{
			xFirstPanelRow.Height = new GridLength(0);
		}
    }

}

