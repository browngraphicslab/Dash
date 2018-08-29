using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
using Dash;
using Dash.Annotations;
using MyToolkit.Multimedia;
using static Dash.DataTransferTypeInfo;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    #region Enums and Interfaces

    public interface ISelectable
    {
        void Select();
        void Deselect();

        bool Selected { get; }

        DocumentController RegionDocument { get; }
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
        public readonly RegionGetter       GetRegion;
        private readonly InkController      _inkController;
        public readonly AnnotationManager  AnnotationManager;
        public ISelectable                 SelectedRegion;
        private AnnotationType              _currAnnotationType = AnnotationType.None;
        public readonly List<ISelectable>   Regions = new List<ISelectable>();

        // we store section of selected text in this list of KVPs with the key and value as start and end index, respectively
        //public readonly List<KeyValuePair<int, int>> CurrentSelections = new List<KeyValuePair<int, int>>();
        //public readonly List<Rect>                   CurrentSelectionClipRects = new List<Rect>();
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

            var deselect = SelectedRegion?.Selected == true;
            var selectable = Regions.FirstOrDefault(sel => sel.RegionDocument.Equals(region));
            foreach (var nvo in this.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<NewAnnotationOverlay>())
                foreach (var r in nvo.Regions.Where(r => r.RegionDocument.Equals(selectable?.RegionDocument)))
                {
                    nvo.SelectedRegion?.Deselect();
                    nvo.SelectedRegion = deselect ? null : r;
                    if (!deselect) { 
                        r.Select();
                    }
                    if (documentView.ViewModel != null)
                    {
                        documentView.ViewModel.SearchHighlightState = new Thickness(deselect ? 0 : 8);
                    }
                }
        }

        private void DeselectRegion()
        {
            var selectedRegion = SelectedRegion;
            if (selectedRegion != null)
                foreach (var nvo in this.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<NewAnnotationOverlay>())
                    foreach (var r in nvo.Regions.Where(r => r.RegionDocument.Equals(selectedRegion.RegionDocument)))
                    {
                        nvo.SelectedRegion?.Deselect();
                        nvo.SelectedRegion = null;
                    }
        }

        public NewAnnotationOverlay([NotNull] DocumentController viewDocument, [NotNull] RegionGetter getRegion)
        {
            InitializeComponent();

            MainDocument = viewDocument;
            GetRegion = getRegion;

            AnnotationManager = new AnnotationManager(this);

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
            var newAnnotations = new List<AnchorableAnnotation>();
            switch (documentController.GetAnnotationType())
            {
                // regions and selectons follow the same functionality
                case AnnotationType.Region:
                case AnnotationType.Selection:
                    newAnnotations.Add(new RegionAnnotation(this));
                    newAnnotations.Add(new TextAnnotation(this));
                    newAnnotations.ForEach(i => i.DocumentController = documentController);
                    var rvm = new AnchorableAnnotation.SelectionViewModel(documentController,
                        new SolidColorBrush(Color.FromArgb(0x30, 0xff, 0, 0)),
                        new SolidColorBrush(Color.FromArgb(100, 0xff, 0xff, 0)));
                    newAnnotations.ForEach(i => i.Render(rvm));
                    break;
                case AnnotationType.Ink:
                    break;
                case AnnotationType.Pin:
                    //render pin will be called with specific doc controller if in process of making pin
                    newAnnotations.Add(new PinAnnotation(this));
                    newAnnotations.ForEach(i => i.DocumentController = documentController);
                    var pvm = new AnchorableAnnotation.SelectionViewModel(documentController,
                        new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), new SolidColorBrush(Colors.OrangeRed));
                    newAnnotations.ForEach(i => i.Render(pvm));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public DocumentController MakeAnnotationPinDoc(Point point, DocumentController linkedDoc = null)
        {
            var annotation = GetRegion(AnnotationType.Pin);
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

        public DocumentController GetRegionDoc(bool addToList = true)
        {
            if (SelectedRegion != null)
            {
                return SelectedRegion.RegionDocument;
            }

            DocumentController annotation = null;
            switch (CurrentAnnotationType)
            {
                case AnnotationType.Region:
                case AnnotationType.Selection:
                    if (!CurrentAnchorableAnnotations.Any() || CurrentAnchorableAnnotations.Any(i =>
                            (i as RegionAnnotation)?.Width < 10 && (i as RegionAnnotation)?.Height < 10))
                    {
                        ClearSelection(true);
                        goto case AnnotationType.None;
                    }

                    annotation = GetRegion(CurrentAnnotationType);

                    var subRegionsOffsets = new List<double>();
                    double minRegionY = double.PositiveInfinity;
                    foreach (var item in CurrentAnchorableAnnotations)
                    {
                        var vOffset = item.AddSubregionToRegion(annotation);
                        subRegionsOffsets.Add(vOffset);
                        minRegionY = Math.Min(minRegionY, vOffset);
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
                default:
                    return;
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

        [CanBeNull] private AnchorableAnnotation _currentAnnotation;
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

        /// <summary>
        /// Call this method with a null target if you just want to make a pushpin annotation with the default text.
        /// Pass in a target to create a pushpin annotation with a document controller intended as the target.
        /// </summary>
        /// <param name="point"></param>
        public void CreatePin(Point point, DocumentController target = null)
        {
            _currentAnnotation = new PinAnnotation(this, point, target);
        }

        #endregion

        #region Selection Annotation

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
            CommittedAnchorableAnnotations.AddRange(CurrentAnchorableAnnotations);
            CurrentAnchorableAnnotations.Clear();
            _selectionStartPoint = hardReset ? null : _selectionStartPoint;
            _selectedRectangles.Clear();
            XSelectionCanvas.Children.Clear();
            XPreviewRect.Width = XPreviewRect.Height = 0;
            var removeItems = XAnnotationCanvas.Children.Where(i => !((i as FrameworkElement)?.DataContext is AnchorableAnnotation.SelectionViewModel) && i != XPreviewRect).ToList();
            if (XAnnotationCanvas.Children.Any())
            {
                var lastAdded = XAnnotationCanvas.Children.Last();
                if (!((lastAdded as FrameworkElement)?.DataContext is AnchorableAnnotation.SelectionViewModel))
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

        public List<AnchorableAnnotation> CurrentAnchorableAnnotations = new List<AnchorableAnnotation>();
        public List<AnchorableAnnotation> CommittedAnchorableAnnotations = new List<AnchorableAnnotation>();

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
            if (_currentAnnotation is TextAnnotation textAnnotation)
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

                var currentSelectionStart = textAnnotation.StartIndex;
                var currentSelectionEnd = textAnnotation.EndIndex;
                var currentClipRect = textAnnotation.ClipRect;

                textAnnotation.ClipRect = this.IsAltPressed() ?
                    new Rect(new Point(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y)),
                             new Point(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y))) :
                    Rect.Empty;
                if (this.IsAltPressed())
                {
                    for (var i = currentSelectionStart; i <= currentSelectionEnd; ++i)
                    {
                        DeselectIndex(i, currentClipRect);
                    }
                    for (var i = startIndex; i <= endIndex; ++i)
                    {
                        SelectIndex(i, currentClipRect);
                    }
                }
                else
                {
                    if (currentSelectionStart == -1 || (currentClipRect != null && currentClipRect != Rect.Empty))
                    {
                        for (var i = startIndex; i <= endIndex; ++i)
                        {
                            SelectIndex(i, currentClipRect);
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

                textAnnotation.StartIndex = startIndex;
                textAnnotation.EndIndex = endIndex;
            }
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
            if (e.DataView.HasDataOfType(Internal))
            {
                if (!this.IsShiftPressed())
                {
                    var dragLayoutDoc = e.DataView.GetDragModels().OfType<DragDocumentModel>().SelectMany((dm) => dm.DraggedDocuments).FirstOrDefault();
                    foreach (var rdoc in RegionDocsList.Where((rd) => rd.GetAnnotationType() == AnnotationType.Pin))
                        if (dragLayoutDoc.GetDataDocument().Equals(rdoc.GetDataDocument().GetLinks(KeyStore.LinkToKey).FirstOrDefault().GetDataDocument().
                                  GetLinkedDocument((LinkDirection.ToDestination)).GetDataDocument()))
                        {
                            dragLayoutDoc.SetPosition(e.GetPosition(this));
                            e.AcceptedOperation = DataPackageOperation.None;
                            e.Handled = true;
                        }
                }
                else
                {
                    var targets = await e.DataView.GetDroppableDocumentsForDataOfType(Internal, sender as FrameworkElement, where);

                    foreach (var doc in targets)
                    {
                        doc.SetBackgroundColor(Colors.White);
                        if (!doc.DocumentType.Equals(RichTextBox.DocumentType) &&
                            !doc.DocumentType.Equals(TextingBox.DocumentType))
                        {
                            if (doc.GetActualSize()?.X > 200)
                            {
                                double ratio = doc.GetHeight() / doc.GetWidth();
                                doc.SetWidth(200);
                                doc.SetHeight(200 * ratio);
                            }
                        }

                        CreatePin(where, doc);
                    }

                    e.Handled = true;
                }
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
