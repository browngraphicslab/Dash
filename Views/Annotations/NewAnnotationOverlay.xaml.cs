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
        bool IsSelected { get; set;  }

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
        private readonly InkController      _inkController;
        private AnnotationType              _currAnnotationType = AnnotationType.None;
        private bool                        _maskInkUpdates;

        public readonly DocumentController   MainDocument;
        public readonly RegionGetter         GetRegion;
        public readonly AnnotationManager    AnnotationManager;
        public ISelectable                   SelectedRegion;
        public readonly ListController<DocumentController> RegionDocsList;

        public delegate DocumentController RegionGetter(AnnotationType type);

        public NewAnnotationOverlayViewModel ViewModel => DataContext as NewAnnotationOverlayViewModel;
        public AnnotationType                CurrentAnnotationType
        {
            get =>_currAnnotationType;
            set
            {
                _currAnnotationType = value;
                XInkCanvas.InkPresenter.IsInputEnabled = _currAnnotationType == AnnotationType.Ink;
                XInkCanvas.IsHitTestVisible = _currAnnotationType == AnnotationType.Ink;
            }
        }

        public NewAnnotationOverlay([NotNull] DocumentController viewDocument, [NotNull] RegionGetter getRegion)
        {
            InitializeComponent();

            MainDocument = viewDocument;
            GetRegion = getRegion;

            AnnotationManager = new AnnotationManager(this);

            RegionDocsList = MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);
            _inkController = MainDocument.GetDataDocument().GetFieldOrCreateDefault<InkController>(KeyStore.InkDataKey);

            XInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            XInkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            XInkCanvas.InkPresenter.IsInputEnabled = false;
            XInkCanvas.IsHitTestVisible = false;
            XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
            Loaded += onLoaded;
            Unloaded += onUnloaded;
        }

        public IEnumerable<ISelectable>     SelectableRegions => XAnnotationCanvas.Children.OfType<AnchorableAnnotation>().Where((a) => a.ViewModel != null).Select((a)=>a.ViewModel);

        public void SelectRegion(DocumentController region)
        {
            var documentView = this.GetFirstAncestorOfType<DocumentView>();
            documentView.Visibility = Visibility.Visible;

            var deselect = SelectedRegion?.IsSelected == true;
            var selectable = SelectableRegions.FirstOrDefault(sel => sel.RegionDocument.Equals(region));
            foreach (var nvo in this.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<NewAnnotationOverlay>())
                foreach (var r in nvo.SelectableRegions.Where(r => r.RegionDocument.Equals(selectable?.RegionDocument)))
                {
                    nvo.SelectedRegion.IsSelected = false;
                    nvo.SelectedRegion = deselect ? null : r;
                    if (!deselect) {
                        r.IsSelected = true;
                    }
                    if (documentView.ViewModel != null)
                    {
                        documentView.ViewModel.SearchHighlightState = new Thickness(deselect ? 0 : 8);
                    }
                }
        }
        public void DeselectRegion()
        {
            var selectedRegion = SelectedRegion;
            if (selectedRegion != null)
                foreach (var nvo in this.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<NewAnnotationOverlay>())
                    foreach (var r in nvo.SelectableRegions.Where(r => r.RegionDocument.Equals(selectedRegion.RegionDocument)))
                    {
                        nvo.SelectedRegion.IsSelected = false;
                        nvo.SelectedRegion = null;
                    }
        }
        /// <summary>
        /// Returns any active (selected or preview) region document
        /// </summary>
        /// <returns></returns>
        public DocumentController GetRegionDoc()
        {
            var annotation = SelectedRegion?.RegionDocument;
            if (annotation == null &&
                (CurrentAnnotationType == AnnotationType.Region || CurrentAnnotationType == AnnotationType.Selection))
            {
                if (CurrentAnchorableAnnotations.Any() &&
                    !CurrentAnchorableAnnotations.OfType<RegionAnnotation>().Any(i => i?.Width < 10 && i?.Height < 10))
                {
                    annotation = GetRegion(CurrentAnnotationType);

                    var subRegionsOffsets = CurrentAnchorableAnnotations.Select((item) => item.AddSubregionToRegion(annotation)).ToList();
                    subRegionsOffsets.Sort((y1, y2) => Math.Sign(y1 - y2));

                    annotation.GetDataDocument().SetPosition(new Point(0, subRegionsOffsets.FirstOrDefault()));
                    annotation.SetRegionDefinition(MainDocument);
                    annotation.SetAnnotationType(CurrentAnnotationType);
                    RegionDocsList.Add(annotation);
                }
                ClearSelection(true);
            }

            return annotation;
        }
        public DocumentController CreatePinRegion(Point point, DocumentController linkedDoc = null)
        {
            var annotation = GetRegion(AnnotationType.Pin);
            annotation.SetPosition(point);
            annotation.SetWidth(10);
            annotation.SetHeight(10);
            annotation.GetDataDocument().SetAnnotationType(AnnotationType.Pin);
            annotation.GetDataDocument().SetRegionDefinition(MainDocument);
            if (linkedDoc != null)
            {
                annotation.Link(linkedDoc, LinkBehavior.Overlay, null);
            }

            RegionDocsList.Add(annotation);
            return annotation;
        }
        public void LoadEmbeddedAnnotations()
        {
            ViewModel.ViewModels.Clear();

            var pinAnnotations = MainDocument.GetDataDocument()
                .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.PinAnnotationsKey);
            foreach (var doc in pinAnnotations)
            {
                ViewModel.ViewModels.Add(new DocumentViewModel(doc)
                    {
                        Undecorated = true,
                        ResizersVisible = true,
                        DragBounds = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) }
                    });
            }
        }

        void onUnloaded(object o, RoutedEventArgs routedEventArgs)
        {
            RegionDocsList.FieldModelUpdated -= regionDocsListOnFieldModelUpdated;
            _inkController.FieldModelUpdated -= inkController_FieldModelUpdated;
        }

        void onLoaded(object o, RoutedEventArgs routedEventArgs)
        {
            _inkController.FieldModelUpdated += inkController_FieldModelUpdated;
            RegionDocsList.FieldModelUpdated += regionDocsListOnFieldModelUpdated;
            xItemsControl.ItemsSource = ViewModel.ViewModels;
        }

        void regionDocsListOnFieldModelUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs args, Context c)
        {
            if ((args is ListController<DocumentController>.ListFieldUpdatedEventArgs listArgs) &&
                 listArgs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add)
                listArgs.NewItems.ForEach((reg) => XAnnotationCanvas.Children.Add(AnchorableAnnotation.CreateAnnotation(this, reg)));
        }
        void inkController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (!_maskInkUpdates)
            {
                XInkCanvas.InkPresenter.StrokeContainer.Clear();
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
            }
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
                linkSource.Link(linkTarget, LinkBehavior.Zoom, linkTag);
            else linkSource.Link(linkTarget, LinkBehavior.Zoom);

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


        /// <summary>
        /// Call this method with a null target if you just want to make a pushpin annotation with the default text.
        /// Pass in a target to create a pushpin annotation with a document controller intended as the target.
        /// </summary>
        /// <param name="point"></param>
        public async void EmbedDocumentWithPin(Point point, DocumentController target = null)
        {
            _currentAnnotation = XAnnotationCanvas.Children.OfType<PinAnnotation>().Where((pin) => pin.GetBoundingRect(this).Contains(point)).FirstOrDefault();
            if (_currentAnnotation == null)
            {
                var targetAnnotation = target ?? await createTarget(this, point);
                embedDocument(targetAnnotation);
                StartAnnotation(AnnotationType.Pin, point, new AnchorableAnnotation.SelectionViewModel(CreatePinRegion(point, targetAnnotation)));
            }

            void embedDocument(DocumentController targetAnnotation)
            { 
                ViewModel.ViewModels.Add(
                    new DocumentViewModel(targetAnnotation)
                    {
                        Undecorated = true,
                        ResizersVisible = true,
                        DragBounds = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) }
                    }
                );

                MainDocument.GetDataDocument()
                    .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.PinAnnotationsKey)
                    .Add(targetAnnotation);
            }
        }
        public void StartAnnotation(AnnotationType type, Point p, AnchorableAnnotation.SelectionViewModel svm = null)
        {
            ClearPreviewRegion();
            switch (type)
            {
                case AnnotationType.Pin:       _currentAnnotation = new PinAnnotation(this, svm); break;
                case AnnotationType.Region:    _currentAnnotation = new RegionAnnotation(this, svm); break;
                case AnnotationType.Selection: _currentAnnotation = new TextAnnotation(this, svm); break;
            }
            _currentAnnotation?.StartAnnotation(p);
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

        /// <summary>
        /// Creates a target annotation for a pushpin.  If Ctrl is pressed, then the user can choose the type of annotation, 
        /// otherwise the default is text.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        static async Task<DocumentController> createTarget(NewAnnotationOverlay parent, Point where)
        {
            DocumentController target = null;
            // the user can gain more control over what kind of pushpin annotation they want to make by holding control, which triggers a popup
            switch (parent.IsCtrlPressed() ? await MainPage.Instance.GetPushpinType() : PushpinType.Text)
            {
                case PushpinType.Text:
                    {
                        var richText = new RichTextNote("<annotation>", new Point(where.X + 10, where.Y + 10), new Size(150, 75));
                        richText.Document.SetField(KeyStore.BackgroundColorKey, new TextController(Colors.White.ToString()), true);
                        return richText.Document;
                    }
                case PushpinType.Video: target = await MainPage.Instance.GetVideoFile(); break;
                case PushpinType.Image: target = await MainPage.Instance.GetImageFile(); break;
            }

            target?.SetWidth(250);
            target?.SetHeight(200);
            target?.SetPosition(new Point(where.X + 10, where.Y + 10));
            return target;
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

        #endregion

        #region Selection Annotation

        public List<SelectableElement> TextSelectableElements;

        public void SetSelectableElements(IEnumerable<SelectableElement> selectableElements)
        {
            TextSelectableElements = selectableElements.ToList();

            RegionDocsList.ToList().ForEach((reg) =>
               XAnnotationCanvas.Children.Add(AnchorableAnnotation.CreateAnnotation(this, reg)));
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
            if (linkDoc.GetDataDocument().GetLinkBehavior() == LinkBehavior.Overlay  &&
                RegionDocsList.Contains(linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey)))
            {
                var dest = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey);
                dest.ToggleHidden();

                return LinkHandledResult.HandledClose;
            }

            return LinkHandledResult.Unhandled;
        }

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
                    // if docs are being moved within the overlay, then they will be placed appropriately and returned from this call.
                    // if docs are being dragged onto this overlay, we disallow that and no droppedDocs are returned from this call.
                    var droppedDocs = await e.DataView.GetDroppableDocumentsForDataOfType(Any, sender as FrameworkElement, where);
                    e.AcceptedOperation = droppedDocs.Count > 0 ? DataPackageOperation.Move : DataPackageOperation.None;
                    e.Handled = e.AcceptedOperation != DataPackageOperation.None;
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

                        EmbedDocumentWithPin(where, doc);
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
                        EmbedDocumentWithPin(where, target);
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
