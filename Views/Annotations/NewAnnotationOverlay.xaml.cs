using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using MyToolkit.Multimedia;
using static Dash.DataTransferTypeInfo;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    #region Enums and Interfaces
    public enum AnnotationType
    {
        None,
        Region,
        Selection,
        Ink,
        Pin 
    }

	public enum PushpinType
	{
		Text,
		Video,
		Image
	}

    public interface ISelectable
    {
        void Select();
        void Deselect();

        bool Selected { get; }

        DocumentController RegionDocument { get; }
    }

    public interface IAnchorable
    {
        void Render();
        void StartAnnotation(Point p);
        void UpdateAnnotation(Point p);
        void EndAnnotation(Point p);
        double AddSubregionToRegion(DocumentController region);
    }
    #endregion

    public class NewAnnotationOverlayViewModel : ViewModelBase
    {
        public ObservableCollection<DocumentViewModel> ViewModels { get; set; }
        // should also add all of the annotations in here as their own view model...
        public NewAnnotationOverlayViewModel()
        {
            ViewModels = new ObservableCollection<DocumentViewModel>();
        }
    }

    public sealed partial class NewAnnotationOverlay : UserControl, ILinkHandler
    {
        public delegate DocumentController RegionGetter(AnnotationType type);

        public readonly DocumentController  MainDocument;
        private readonly RegionGetter       _regionGetter;
        private readonly InkController      _inkController;
        private readonly AnnotationManager  _annotationManager;
        private ISelectable                 _selectedRegion;
        private AnnotationType              _currAnnotationType = AnnotationType.None;
        public readonly List<ISelectable>   Regions = new List<ISelectable>();

        // we store section of selected text in this list of KVPs with the key and value as start and end index, respectively
        public readonly List<KeyValuePair<int, int>> CurrentSelections = new List<KeyValuePair<int, int>>();
        public readonly List<Rect>                   CurrentSelectionClipRects = new List<Rect>();
        public static readonly DependencyProperty AnnotationVisibilityProperty = DependencyProperty.Register(
            "AnnotationVisibility", typeof(bool), typeof(NewAnnotationOverlay), new PropertyMetadata(true));

        public readonly ListController<DocumentController> RegionDocsList;
        public bool AnnotationVisibility
        {
            get { return (bool) GetValue(AnnotationVisibilityProperty); }
            set { SetValue(AnnotationVisibilityProperty, value); }
        }

        public AnnotationType CurrentAnnotationType
        {
            get
            {
                return _currAnnotationType;
            }
            set
            {
                _currAnnotationType = value;
                XInkCanvas.InkPresenter.IsInputEnabled = _currAnnotationType == AnnotationType.Ink;
                XInkCanvas.IsHitTestVisible = _currAnnotationType == AnnotationType.Ink;
            }
        }
        

        public void SelectRegion(DocumentController region)
        {
            var documentView = this.GetFirstAncestorOfType<DocumentView>();
            documentView.Visibility = Visibility.Visible;

            var deselect = _selectedRegion?.Selected == true;
            var selectable = Regions.FirstOrDefault(sel => sel.RegionDocument.Equals(region));
            foreach (var nvo in this.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<NewAnnotationOverlay>())
                foreach (var r in nvo.Regions.Where(r => r.RegionDocument.Equals(selectable.RegionDocument)))
                {
                    nvo._selectedRegion?.Deselect();
                    nvo._selectedRegion = deselect ? null : r;
                    if (!deselect) { 
                        r.Select();
                    }
                    if (documentView.ViewModel != null)
                    {
                        documentView.ViewModel.SearchHighlightState = new Thickness(deselect ? 0 : 8);
                    }
                }
        }

        public void SelectRegion(ISelectable selectable, Point? mousePos)
        {
            // get the list of linkhandlers starting from this all the way up to the mainpage
            var linkHandlers = this.GetAncestorsOfType<ILinkHandler>().ToList();
            // NewAnnotationOverlay is an ILinkHandler but isn't included in GetAncestorsOfType()
            linkHandlers.Insert(0, this);
            _annotationManager.FollowRegion(selectable.RegionDocument, linkHandlers, mousePos ?? new Point(0, 0));

            // we still want to follow the region even if it's already selected, so this code's position matters
            if (_selectedRegion != selectable)
            {
                foreach (var nvo in this.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<NewAnnotationOverlay>())
                    foreach (var r in nvo.Regions.Where(r => r.RegionDocument.Equals(selectable.RegionDocument)))
                    {
                        nvo._selectedRegion?.Deselect();
                        nvo._selectedRegion = r;
                        r.Select();
                    }
            }
        }

        private void DeselectRegion()
        {
            var selectedRegion = _selectedRegion;
            if (selectedRegion != null)
                foreach (var nvo in this.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<NewAnnotationOverlay>())
                    foreach (var r in nvo.Regions.Where(r => r.RegionDocument.Equals(selectedRegion.RegionDocument)))
                    {
                        nvo._selectedRegion?.Deselect();
                        nvo._selectedRegion = null;
                    }
        }

        public NewAnnotationOverlay([NotNull] DocumentController viewDocument, [NotNull] RegionGetter regionGetter)
        {
            InitializeComponent();

            MainDocument = viewDocument;
            _regionGetter = regionGetter;

            _annotationManager = new AnnotationManager(this);

            RegionDocsList =
                MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);
            _inkController = MainDocument.GetDataDocument()
                .GetFieldOrCreateDefault<InkController>(KeyStore.InkDataKey);


            XInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            XInkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            XInkCanvas.InkPresenter.IsInputEnabled = false;
            XInkCanvas.IsHitTestVisible = false;
            XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void RenderAnnotation(DocumentController documentController)
        {
            switch (documentController.GetAnnotationType())
            {
                // regions and selectons follow the same functionality
                case AnnotationType.Region:
                    var newRegion = new RegionAnnotation(this)
                    {
                        DocumentController = documentController
                    };
                    newRegion.Render();
                    break;
                case AnnotationType.Selection:
                    var newSelection = new TextAnnotation(this)
                    {
                        DocumentController = documentController
                    };
                    newSelection.Render();
                    break;
                case AnnotationType.Ink:
                    break;
                case AnnotationType.Pin:
					//render pin will be called with specific doc controller if in process of making pin
                    var newPin = new PinAnnotation(this)
                    {
                        DocumentController = documentController
                    };
                    newPin.Render();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnUnloaded(object o, RoutedEventArgs routedEventArgs)
        {
            RegionDocsList.FieldModelUpdated -= RegionDocsListOnFieldModelUpdated;
            _inkController.FieldModelUpdated -= _inkController_FieldModelUpdated;
        }

        private void OnLoaded(object o, RoutedEventArgs routedEventArgs)
        {
            _inkController.FieldModelUpdated += _inkController_FieldModelUpdated;
            RegionDocsList.FieldModelUpdated += RegionDocsListOnFieldModelUpdated;
            xItemsControl.ItemsSource = (DataContext as NewAnnotationOverlayViewModel).ViewModels;
        }

        public void LoadPinAnnotations(PdfView pdfView)
        {
            (DataContext as NewAnnotationOverlayViewModel).ViewModels.Clear();

            if (pdfView != null)
            {
                var pinAnnotations = MainDocument.GetDataDocument()
                    .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.PinAnnotationsKey);
                foreach (var doc in pinAnnotations)
                {
                    var dvm = new DocumentViewModel(doc)
                    {
                        Undecorated = true,
                        ResizersVisible = true,
                        DragBounds =
                            new RectangleGeometry {Rect = new Rect(0, 0, pdfView.PdfMaxWidth, pdfView.PdfTotalHeight)}
                    };
                    (DataContext as NewAnnotationOverlayViewModel).ViewModels.Add(dvm);
                }
            }
        }

        private void RegionDocsListOnFieldModelUpdated(FieldControllerBase fieldControllerBase,
            FieldUpdatedEventArgs fieldUpdatedEventArgs, Context context)
        {
            if (!(fieldUpdatedEventArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs listArgs)
            ) return;

            switch (listArgs.ListAction)
            {
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                    foreach (DocumentController documentController in listArgs.NewItems)
                    {
                        RenderAnnotation(documentController);
                    }

                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace:
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear:
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool _maskInkUpdates;
        private void _inkController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (!_maskInkUpdates)
            {
                XInkCanvas.InkPresenter.StrokeContainer.Clear();
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
            }
        }

        public readonly List<Rect> RegionRectangles = new List<Rect>();
        public List<int> Indices = new List<int>();
        public DocumentController GetRegionDoc(bool addToList = true)
        {
            if (_selectedRegion != null)
            {
                return _selectedRegion.RegionDocument;
            }

            DocumentController annotation = null;
            switch (CurrentAnnotationType)
            {
                case AnnotationType.Region:
                case AnnotationType.Selection:
                    if (!RegionRectangles.Any(rect => rect.Width > 10 && rect.Height > 10) &&
                        (!CurrentSelections.Any() || CurrentSelections.Last().Key == -1))
                    {
                        ClearSelection(true);
                        goto case AnnotationType.None;
                    }

                    //Indices.Clear();
                    //annotation = _regionGetter(CurrentAnnotationType);
                    //annotation.SetField(KeyStore.SelectionRegionTopLeftKey, new ListController<PointController>(), true);
                    //annotation.SetField(KeyStore.SelectionRegionSizeKey, new ListController<PointController>(), true);

                    var subRegionsOffsets = new List<double>();
                    double minRegionY = double.PositiveInfinity;
                    //foreach (var item in AnchorableAnnotations)
                    //{
                    //    var scrollRatio = item.AddSubregionToRegion(annotation);
                    //    subRegionsOffsets.Add(scrollRatio);
                    //    var pdfView = this.GetFirstAncestorOfType<PdfView>();
                    //    var scale = pdfView?.ActualWidth / pdfView?.PdfMaxWidth ?? 1;
                    //    minRegionY = Math.Min(minRegionY, (scrollRatio * pdfView.TopScrollViewer.ExtentHeight) / scale);
                    //}

                    var regionPosList = new ListController<PointController>();
                    var regionSizeList = new ListController<PointController>();
                    var selectionIndexList = new ListController<PointController>();

                    foreach (Rect rect in RegionRectangles)
                    {
                        regionPosList.Add(new PointController(rect.X, rect.Y));
                        regionSizeList.Add(new PointController(rect.Width, rect.Height));
                        var pdfView = this.GetFirstAncestorOfType<PdfView>();
                        var scale = pdfView?.ActualWidth / pdfView?.PdfMaxWidth ?? 1;
                        var vOffset = rect.Y * scale;
                        var scrollRatio = vOffset / pdfView?.TopScrollViewer.ExtentHeight ?? 0;
                        Debug.Assert(!double.IsNaN(scrollRatio));
                        subRegionsOffsets.Add(scrollRatio);
                        minRegionY = Math.Min(rect.Y, minRegionY);
                    }

                    //loop through each selection and add the indices in each selection set
                    foreach (var selection in CurrentSelections)
                    {
                        var ind = CurrentSelections.IndexOf(selection);
                        for (var i = selection.Key; i <= selection.Value; i++)
                        {
                            var elem = TextSelectableElements[i];
                            if (CurrentSelectionClipRects[ind] == Rect.Empty || CurrentSelectionClipRects[ind]
                                    .Contains(new Point(elem.Bounds.X + elem.Bounds.Width / 2,
                                        elem.Bounds.Y + elem.Bounds.Height / 2)))
                            {
                                // this will avoid double selecting any items
                                if (!Indices.Contains(i))
                                {
                                    Indices.Add(i);
                                }
                            }
                        }

                        selectionIndexList.Add(new PointController(selection.Key, selection.Value));
                    }
                    annotation.SetField(KeyStore.SelectionRegionTopLeftKey, regionPosList, true);
                    annotation.SetField(KeyStore.SelectionRegionSizeKey, regionSizeList, true);

                    int prevIndex = -1;
                    foreach (int index in Indices)
                    {
                        SelectableElement elem = TextSelectableElements[index];
                        if (prevIndex + 1 != index)
                        {
                            var pdfView = this.GetFirstAncestorOfType<PdfView>();
                            var scale = pdfView.ActualWidth / pdfView.PdfMaxWidth;
                            var vOffset = elem.Bounds.Y * scale;
                            var scrollRatio = vOffset / pdfView.TopScrollViewer.ExtentHeight;
                            Debug.Assert(!double.IsNaN(scrollRatio));
                            subRegionsOffsets.Add(scrollRatio);
                        }

                        minRegionY = Math.Min(minRegionY, elem.Bounds.Y);
                        prevIndex = index;
                    }

                    subRegionsOffsets.Sort((y1, y2) => Math.Sign(y1 - y2));

                    if (this.GetFirstAncestorOfType<PdfView>() != null)
                    {
                        annotation.SetField(KeyStore.PDFSubregionKey,
                            new ListController<NumberController>(
                                subRegionsOffsets.ConvertAll(i => new NumberController(i))), true);
                    }

                    annotation.GetDataDocument().SetPosition(new Point(0, minRegionY));
                    ClearSelection(true);
                    break;
                case AnnotationType.Ink:
                case AnnotationType.None:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Debug.Assert(annotation != null, "Annotation must be assigned in the switch statement");
            Debug.Assert(!annotation.Equals(MainDocument),
                "If returning the main document, return it immediately, don't fall through to here");
            annotation.SetRegionDefinition(MainDocument);
            annotation.SetAnnotationType(CurrentAnnotationType);
            RegionDocsList.Add(annotation);

            return annotation;
        }

        public static void LinkRegion(DocumentController sourceDoc, DocumentController targetDoc,
            double? sStartIndex = null, double? sEndIndex = null, double? tStartIndex = null, double? tEndIndex = null,
            string linkTag = null)
        {
            Debug.Assert(sourceDoc.GetRegionDefinition() == null);
            var linkSource = (sStartIndex is double sStart && sEndIndex is double sEnd)
                ? createRegionDoc(sourceDoc, sStart, sEnd)
                : sourceDoc;
            var linkTarget = (tStartIndex is double tStart && tEndIndex is double tEnd)
                ? createRegionDoc(targetDoc, tStart, tEnd)
                : targetDoc;

            if (linkTag != null)
                linkSource.Link(linkTarget, LinkTargetPlacement.Default, linkTag);
            else linkSource.Link(linkTarget, LinkTargetPlacement.Default);

            DocumentController createRegionDoc(DocumentController regionContainerDocument, double start, double end)
            {
                var region = ExistingRegionAtIndices(regionContainerDocument, start, end);
                if (region == null)
                {
                    region = new RichTextNote().Document;
                    region.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionIndicesListKey)
                        .Add(new PointController(start, end));
                    region.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
                    region.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);
                    region.SetAnnotationType(AnnotationType.Selection);
                    region.SetRegionDefinition(regionContainerDocument);

                    regionContainerDocument.GetDataDocument()
                        .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey).Add(region);
                }

                return region;
            }

            DocumentController ExistingRegionAtIndices(DocumentController doc, double startIndex, double endIndex)
            {
                return doc.GetDataDocument().GetRegions().FirstOrDefault(reg =>
                {
                    var selectionIndices =
                        reg.GetField<ListController<PointController>>(KeyStore.SelectionIndicesListKey);
                    if (selectionIndices.Count == 1)
                    {
                        if ((int) startIndex == (int) selectionIndices[0].Data.X &&
                            (int) endIndex == (int) selectionIndices[0].Data.Y)
                            return true;
                    }

                    return false;
                });
            }
        }


        #region General Annotation

        public void StartAnnotation(Point p)
        {
            ClearPreviewRegion();
            switch (CurrentAnnotationType)
            {
                case AnnotationType.Region:
                    _currentAnnotation = new RegionAnnotation(this);
                    break;
                case AnnotationType.Selection:
                    _currentAnnotation = new TextAnnotation(this);
                    break;
            }
            _currentAnnotation.StartAnnotation(p);
        }

        public void UpdateAnnotation(Point p)
        {
            _currentAnnotation?.UpdateAnnotation(p);
        }

        public void EndAnnotation(Point p)
        {
            if (_currentAnnotation != null)
            {
                _currentAnnotation.EndAnnotation(p);
                _currentAnnotation = null;
                DeselectRegion();
            }
        }

        [CanBeNull] private IAnchorable _currentAnnotation;
        #endregion

        #region Ink Annotation

        private void InkPresenterOnStrokesErased(InkPresenter inkPresenter, InkStrokesErasedEventArgs inkStrokesErasedEventArgs)
        {
            _maskInkUpdates = true;
            _inkController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            _maskInkUpdates = false;
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            _maskInkUpdates = true;
            _inkController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            _maskInkUpdates = false;
        }

        #endregion

        #region Region Annotation

        public void ClearPreviewRegion()
        {
            XPreviewRect.Visibility = Visibility.Collapsed;
        }

        public DocumentController MakeAnnotationPinDoc(Point point, DocumentController linkedDoc = null)
        {
			var annotation = _regionGetter(AnnotationType.Pin);
		    annotation.SetPosition(new Point(point.X + 10, point.Y + 10));
            if (this.GetFirstAncestorOfType<PdfView>() != null)
                annotation.SetField(KeyStore.PDFSubregionKey,
                    new ListController<NumberController>
                    {
                        new NumberController((point.Y + 10) / this.GetFirstAncestorOfType<PdfView>().PdfTotalHeight)
                    }, true);
		    annotation.SetWidth(10);
		    annotation.SetHeight(10);
		    annotation.GetDataDocument().SetField<TextController>(KeyStore.RegionTypeKey, nameof(AnnotationType.Pin), true);
            annotation.GetDataDocument().SetRegionDefinition(MainDocument);
            if (linkedDoc != null)
            {
                annotation.Link(linkedDoc, LinkTargetPlacement.Overlay);
            }

            RegionDocsList.Add(annotation);
            //format pin annotation
            return annotation;
        }

        /// <summary>
        /// Call this method if you just want to make a pushpin annotation with the default text.
        /// </summary>
        /// <param name="point"></param>
        public async void CreatePin(Point point)
        {
            foreach (var region in XAnnotationCanvas.Children)
            {
                if (region is Ellipse existingPin && existingPin.GetBoundingRect(this).Contains(point))
                {
                    return;
                }
            }

	        DocumentController annotationController;

	        var pdfView = this.GetFirstAncestorOfType<PdfView>();
	        var scale = pdfView.Width / pdfView.PdfMaxWidth;

			// the user can gain more control over what kind of pushpin annotation they want to make by holding control, which triggers a popup
			if (this.IsCtrlPressed())
	        {
		        var pushpinType = await MainPage.Instance.GetPushpinType();
		        switch (pushpinType)
		        {
					case PushpinType.Text:
						annotationController = CreateTextPin(point);
						break;
					case PushpinType.Video:
						annotationController = await CreateVideoPin(point);
						break;
					case PushpinType.Image:
						annotationController = await CreateImagePin(point);
						break;
					default:
						throw new ArgumentOutOfRangeException();
		        }
			}
	        else
	        {
				// by default the pushpin will create a text note
		        annotationController = CreateTextPin(point);
	        }

			// if the user presses back or cancel, return null
	        if (annotationController == null)
	        {
		        return;
			}

	        CreatePin(point, annotationController);
        }

		/// <summary>
		/// Call this method if you want to make a pushpin annotation with a DocumentController in mind as the target.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="target"></param>
	    private void CreatePin(Point point, DocumentController target)
		{
		    _currentAnnotation = new PinAnnotation(this, target)
		    {
		        DocumentController = MakeAnnotationPinDoc(point, target)
		    };
        }

        private async Task<DocumentController> CreateVideoPin(Point point)
	    {
		    var video = await MainPage.Instance.GetVideoFile();
		    if (video == null) return null;

		    DocumentController videoNote = null;

			// we may get a URL or a storage file -- I had a hard time with getting a StorageFile from a URI, so unfortunately right now they're separated
		    switch (video.Type)
		    {
				case VideoType.StorageFile:
					videoNote = await new VideoToDashUtil().ParseFileAsync(video.File);
					break;
				case VideoType.Uri:
					var query = HttpUtility.ParseQueryString(video.Uri.Query);
					var videoId = string.Empty;

					if (query.AllKeys.Contains("v"))
					{
						videoId = query["v"];
					}
					else
					{
						videoId = video.Uri.Segments.Last();
					}

					try
					{
						var url = await YouTube.GetVideoUriAsync(videoId, YouTubeQuality.Quality1080P);
						var uri = url.Uri;
						videoNote = VideoToDashUtil.CreateVideoBoxFromUri(uri);
					}
					catch (Exception)
					{
						// TODO: display error video not found
					}

					break;
		    }

		    if (videoNote == null) return null;

		    videoNote.SetField(KeyStore.LinkTargetPlacement, new TextController(nameof(LinkTargetPlacement.Overlay)), true);
		    videoNote.SetField(KeyStore.WidthFieldKey, new NumberController(250), true);
		    videoNote.SetField(KeyStore.HeightFieldKey, new NumberController(200), true);
		    videoNote.SetField(KeyStore.PositionFieldKey, new PointController(point.X + 10, point.Y + 10), true);

		    return videoNote;
	    }

	    private async Task<DocumentController> CreateImagePin(Point point)
	    {
		    var file = await MainPage.Instance.GetImageFile();
		    if (file == null) return null;

		    var imageNote = await new ImageToDashUtil().ParseFileAsync(file);
		    imageNote.SetField(KeyStore.LinkTargetPlacement, new TextController(nameof(LinkTargetPlacement.Overlay)), true);
		    imageNote.SetField(KeyStore.WidthFieldKey, new NumberController(250), true);
		    imageNote.SetField(KeyStore.HeightFieldKey, new NumberController(200), true);
		    imageNote.SetField(KeyStore.PositionFieldKey, new PointController(point.X + 10, point.Y + 10), true);

		    return imageNote;
	    }

		/// <summary>
		/// Creates a pushpin annotation with a text note, and returns its DocumentController for CreatePin to finish the process.
		/// </summary>
		/// <param name="point"></param>
		private DocumentController CreateTextPin(Point point)
	    {
			var richText = new RichTextNote("<annotation>", new Point(point.X + 10, point.Y + 10),
				new Size(150, 75));
			richText.Document.SetField(KeyStore.BackgroundColorKey, new TextController(Colors.White.ToString()), true);
			richText.Document.SetField(KeyStore.LinkTargetPlacement, new TextController(nameof(LinkTargetPlacement.Overlay)), true);

		    return richText.Document;
		}

        public void FormatRegionOptionsFlyout(DocumentController region, UIElement regionGraphic)
	    {
		    // context menu that toggles whether annotations should be show/ hidden on scroll

		    MenuFlyout flyout = new MenuFlyout();
		    MenuFlyoutItem visOnScrollON = new MenuFlyoutItem();
		    MenuFlyoutItem visOnScrollOFF = new MenuFlyoutItem();
		    visOnScrollON.Text = "Unpin Annotation";
		    visOnScrollOFF.Text = "Pin Annotation";

	        void VisOnScrollOnOnClick(object o, RoutedEventArgs routedEventArgs)
	        {
	            var allLinks   = region.GetDataDocument().GetLinks(null);
	            var allVisible = allLinks.All(doc => doc.GetDataDocument().GetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey)?.Data ?? false);

	            foreach (DocumentController link in allLinks)
	            {
	                link.GetDataDocument().SetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey, !allVisible, true);
	            }
	        }
            visOnScrollON.Click += VisOnScrollOnOnClick;
		    visOnScrollOFF.Click += VisOnScrollOnOnClick;
            regionGraphic.ContextFlyout = flyout;
		    regionGraphic.RightTapped += (s, e) =>
		    {
		        var  allLinks   = region.GetDataDocument().GetLinks(null);
		        bool allVisible = allLinks.All(doc => doc.GetDataDocument().GetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey)?.Data ?? false);

                var item = allVisible ? visOnScrollON : visOnScrollOFF;
			    flyout.Items?.Clear();
			    flyout.Items?.Add(item);
			    flyout.ShowAt(regionGraphic as FrameworkElement);
		    };
		}

        #endregion

        #region Selection Annotation

        public sealed class SelectionViewModel : INotifyPropertyChanged, ISelectable
        {
            private SolidColorBrush _selectionColor;
            public SolidColorBrush SelectionColor
            {
                [UsedImplicitly]
                get => _selectionColor;
                private set
                {
                    if (_selectionColor == value)
                    {
                        return;
                    }
                    _selectionColor = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public SolidColorBrush SelectedBrush { get; set; }

            public SolidColorBrush UnselectedBrush { get; set; }

            public SelectionViewModel(DocumentController region, 
                SolidColorBrush selectedBrush,
                SolidColorBrush unselectedBrush)
            {
                RegionDocument = region;
                UnselectedBrush = unselectedBrush;
                SelectedBrush = selectedBrush;
                _selectionColor = UnselectedBrush;
            }

            public bool Selected { get; private set; }
            
            public DocumentController RegionDocument { get; }

            public void Select()
            {
                SelectionColor = SelectedBrush;
                Selected = true;
            }

            public void Deselect()
            {
                SelectionColor = UnselectedBrush;
                Selected = false;
            }

            [NotifyPropertyChangedInvocator]
            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public List<SelectableElement> TextSelectableElements;

        public void SetSelectableElements(IEnumerable<SelectableElement> selectableElements)
        {
            TextSelectableElements = selectableElements.ToList();

            foreach (var documentController in RegionDocsList)
            {
                RenderAnnotation(documentController);
            }
        }

        public void ClearSelection(bool hardReset = false)
        {
            CurrentSelections.Clear();
            CurrentSelectionClipRects.Clear();
            _selectionStartPoint = hardReset ? null : _selectionStartPoint;
            _selectedRectangles.Clear();
            XSelectionCanvas.Children.Clear();
            XPreviewRect.Width = XPreviewRect.Height = 0;
            RegionRectangles.Clear();
            var removeItems = XAnnotationCanvas.Children.Where(i => !((i as FrameworkElement)?.DataContext is SelectionViewModel) && i != XPreviewRect).ToList();
            if (XAnnotationCanvas.Children.Any())
            {
                var lastAdded = XAnnotationCanvas.Children.Last();
                if (!((lastAdded as FrameworkElement)?.DataContext is SelectionViewModel))
                {
                    removeItems.Add(lastAdded);
                }
            }
            foreach (var item in removeItems)
            {
                if (item != xItemsControl)
                    XAnnotationCanvas.Children.Remove(item);
            }
        }

        public List<IAnchorable> AnchorableAnnotations = new List<IAnchorable>();

        #region Selection Logic

        private void DeselectIndex(int index, Rect? clipRect = null)
        {
            if (_selectedRectangles.ContainsKey(index))
            {
                var ele = TextSelectableElements[index];
                if (clipRect == null || clipRect == Rect.Empty || 
                    clipRect?.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2, ele.Bounds.Y + ele.Bounds.Height / 2)) == true)
                {

                    //XSelectionCanvas.Children.Remove(_selectedRectangles[index]);
                    _selectedRectangles[index].Visibility = Visibility.Collapsed;
                    // _selectedRectangles.Remove(index);
                }
            }
        }

        private readonly SolidColorBrush _selectionBrush = new SolidColorBrush(Color.FromArgb(120, 0x94, 0xA5, 0xBB));

        private void SelectIndex(int index, Rect? clipRect = null)
        {
            var ele = TextSelectableElements[index];
            if (clipRect == null || clipRect == Rect.Empty ||
                clipRect?.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2, ele.Bounds.Y + ele.Bounds.Height / 2)) == true)
            {
                if (!_selectedRectangles.ContainsKey(index))
                {
                    var rect = new Rectangle
                    {
                        Width = ele.Bounds.Width,
                        Height = ele.Bounds.Height
                    };
                    Canvas.SetLeft(rect, ele.Bounds.Left);
                    Canvas.SetTop(rect, ele.Bounds.Top);
                    rect.Fill = _selectionBrush;

                    XSelectionCanvas.Children.Add(rect);

                    _selectedRectangles[index] = rect;
                }
                else
                    _selectedRectangles[index].Visibility = Visibility.Visible;
            }
        }


        private Point? _selectionStartPoint;
        private readonly Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();

        public void SelectElements(int startIndex, int endIndex, Point start, Point end)
        {
            // if control isn't pressed, reset the selection
            if (this.IsAltPressed())
            {
                var bounds = new Rect(new Point(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y)),
                             new Point(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y)));
                foreach (var ele in TextSelectableElements)
                {
                    if (bounds.Contains(new Point(ele.Bounds.Left + ele.Bounds.Width / 2, ele.Bounds.Top + ele.Bounds.Height / 2)))
                    {
                        if (ele.Index < startIndex)
                            startIndex = ele.Index;
                        if (ele.Index > endIndex)
                            endIndex = ele.Index;
                    }
                }
            }
            // if there's no current selections or if there's nothing in the list of selections that matches what we're trying to select
            if (!CurrentSelections.Any() || !CurrentSelections.Any(sel => sel.Key <= startIndex && startIndex <= sel.Value))
            {
                // create a new selection
                CurrentSelections.Add(new KeyValuePair<int, int>(-1, -1));
                CurrentSelectionClipRects.Add(Rect.Empty);
            }
            var currentSelectionStart = CurrentSelections.Last().Key;
            var currentSelectionEnd   = CurrentSelections.Last().Value;
            var lastSelectionClipRect = CurrentSelectionClipRects.LastOrDefault();

            CurrentSelectionClipRects[CurrentSelectionClipRects.Count - 1] = this.IsAltPressed() ? 
                new Rect(new Point(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y)), 
                         new Point(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y))) : 
                Rect.Empty;
            if (this.IsAltPressed())
            {
                for (var i = currentSelectionStart; i <= currentSelectionEnd; ++i)
                {
                    DeselectIndex(i, lastSelectionClipRect);
                }
                for (var i = startIndex; i <= endIndex; ++i)
                {
                    SelectIndex(i, CurrentSelectionClipRects.LastOrDefault());
                }
            }
            else
            {
                if (currentSelectionStart == -1 || (lastSelectionClipRect != null && lastSelectionClipRect != Rect.Empty))
                {
                    for (var i = startIndex; i <= endIndex; ++i)
                    {
                        SelectIndex(i, CurrentSelectionClipRects.LastOrDefault());
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
            }

            // you can't set kvp keys and values, so we have to just create a new one?
            CurrentSelections[CurrentSelections.Count - 1] = new KeyValuePair<int, int>(startIndex, endIndex);
        }

        #endregion

        #endregion

        public LinkHandledResult HandleLink(DocumentController linkDoc, LinkDirection direction)
        {
            if (linkDoc.GetDataDocument().GetField<TextController>(KeyStore.LinkTargetPlacement)?.Data == nameof(LinkTargetPlacement.Overlay) &&
                RegionDocsList.Contains(linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey)))
            {
                var dest = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey);
                dest.ToggleHidden();

                return LinkHandledResult.HandledClose;
            }

            return LinkHandledResult.Unhandled;
        }
        private readonly List<DocumentView> _pinAnnotations = new List<DocumentView>();

	    public void OnDragEnter(object sender, DragEventArgs e)
	    {
		    if (e.DataView.HasDragModels())
		        e.AcceptedOperation |= DataPackageOperation.Copy;
		    else
			    e.AcceptedOperation = DataPackageOperation.None;
	    }

        public async void OnDrop(object sender, DragEventArgs e)
        {
            var where = e.GetPosition(XAnnotationCanvas);
            if (this.IsShiftPressed() && e.DataView.HasDataOfType(Internal))
            {
                var targets = await e.DataView.GetDroppableDocumentsForDataOfType(Internal, sender as FrameworkElement, where);

                foreach (DocumentController doc in targets)
                {
                    doc.SetBackgroundColor(Colors.White);
                    if (!doc.DocumentType.Equals(RichTextBox.DocumentType) && !doc.DocumentType.Equals(TextingBox.DocumentType))
                    {
                        if (doc.GetActualSize()?.X > 200)
                        {
                            double ratio = doc.GetHeight() / doc.GetWidth();
                            doc.SetField(KeyStore.WidthFieldKey, new NumberController(200), true);
                            doc.SetField(KeyStore.HeightFieldKey, new NumberController(200 * ratio), true);
                        }
                    }
                    CreatePin(where, doc);
                }
                e.Handled = true;
            }
            // if we drag from the file system
            if (e.DataView?.Contains(StandardDataFormats.StorageItems) == true)
            {
                e.Handled = true;
                try
                {
                    var target = await FileDropHelper.HandleDrop(e.DataView, where);
                    if (target != null)
                        CreatePin(where, target);
                    if (!target.DocumentType.Equals(RichTextBox.DocumentType) && !target.DocumentType.Equals(TextingBox.DocumentType))
                    {
                        var ratio = target.GetHeight() / target.GetWidth();
                        target.SetField(KeyStore.WidthFieldKey, new NumberController(200), true);
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }
            }

        }
	    
    }

}
