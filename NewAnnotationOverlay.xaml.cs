using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using Dash.Models.DragModels;
using MyToolkit.Multimedia;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Point = Windows.Foundation.Point;
using Syncfusion.Windows.PdfViewer;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public enum AnnotationType
    {
        None,
        Region,
        Selection,
        Ink,
        Pin 
    }


	public enum PinAnnotationVisibility
	{
		VisibleOnScroll,
		ManualToggle,
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

    public class NewAnnotationOverlayViewModel : ViewModelBase
    { 
        public ObservableCollection<DocumentViewModel> ViewModels = new ObservableCollection<DocumentViewModel>();
    }

    public sealed partial class NewAnnotationOverlay : UserControl, ILinkHandler
    {
        private AnnotationType _currentAnnotationType = AnnotationType.None;

        private readonly DocumentController _mainDocument;
        public readonly RegionGetter _regionGetter;
        public readonly ListController<DocumentController> RegionDocsList;
        private readonly InkController _inkController;

        public delegate DocumentController RegionGetter(AnnotationType type);

        private readonly AnnotationManager _annotationManager;

        private ISelectable _selectedRegion;

        public event EventHandler<DocumentController> RegionAdded;
        public event EventHandler<DocumentController> RegionRemoved;

        // we store section of selected text in this list of KVPs with the key and value as start and end index, respectively
        public readonly List<KeyValuePair<int, int>> _currentSelections = new List<KeyValuePair<int, int>>();
        public readonly List<Rect> _currentSelectionClipRects = new List<Rect>();
        public static readonly DependencyProperty AnnotationVisibilityProperty = DependencyProperty.Register(
            "AnnotationVisibility", typeof(bool), typeof(NewAnnotationOverlay), new PropertyMetadata(true));

        public bool AnnotationVisibility
        {
            get { return (bool) GetValue(AnnotationVisibilityProperty); }
            set { SetValue(AnnotationVisibilityProperty, value); }
        }

        public AnnotationType AnnotationType => _currentAnnotationType;

        public List<ISelectable> _regions = new List<ISelectable>();

        public void SelectRegion(DocumentController region)
        {
            var documentView = this.GetFirstAncestorOfType<DocumentView>();
            documentView.Visibility = Visibility.Visible;

            var deselect = _selectedRegion?.Selected == true;
            var selectable = _regions.FirstOrDefault(sel => sel.RegionDocument.Equals(region));
            foreach (var nvo in this.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<NewAnnotationOverlay>())
                foreach (var r in nvo._regions.Where((r) => r.RegionDocument.Equals(selectable.RegionDocument)))
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
                    else
                        ;
                }
        }

        private void SelectRegion(ISelectable selectable, Point? mousePos)
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
                    foreach (var r in nvo._regions.Where((r) => r.RegionDocument.Equals(selectable.RegionDocument)))
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
                    foreach (var r in nvo._regions.Where((r) => r.RegionDocument.Equals(selectedRegion.RegionDocument)))
                    {
                        nvo._selectedRegion?.Deselect();
                        nvo._selectedRegion = null;
                    }
        }

        public NewAnnotationOverlay([NotNull] DocumentController viewDocument, [NotNull] RegionGetter regionGetter)
        {
            this.InitializeComponent();

            _mainDocument = viewDocument;
            _regionGetter = regionGetter;

            _annotationManager = new AnnotationManager(this);

            RegionDocsList =
                _mainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);
            _inkController = _mainDocument.GetDataDocument()
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
                case AnnotationType.Selection:
                    if (documentController.GetField(KeyStore.PDFSubregionKey) == null)
                    {
                        var currentSelections = documentController.GetField<ListController<PointController>>(KeyStore.SelectionIndicesListKey);

                        var indices = new List<int>();
                        double minRegionY = double.PositiveInfinity;
                        foreach (PointController selection in currentSelections)
                        {
                            for (double i = selection.Data.X; i <= selection.Data.Y; i++)
                            {
                                if (!indices.Contains((int)i)) indices.Add((int)i);
                            }
                        }

                        var subRegionsOffsets = new List<double>();
                        int prevIndex = -1;
                        foreach (int index in indices)
                        {
                            SelectableElement elem = _textSelectableElements[index];
                            if (prevIndex + 1 != index)
                            {
                                var pdfView = this.GetFirstAncestorOfType<CustomPdfView>();
                                double scale = pdfView.Width / pdfView.PdfMaxWidth;
                                double vOffset = elem.Bounds.Y * scale;
                                double scrollRatio = vOffset / pdfView.TopScrollViewer.ExtentHeight;
                                subRegionsOffsets.Add(scrollRatio);
                            }
                            minRegionY = Math.Min(minRegionY, elem.Bounds.Y);
                            prevIndex = index;
                        }

                        if ((this.GetFirstAncestorOfType<CustomPdfView>()) != null)
                        {
                            documentController.SetField(KeyStore.PDFSubregionKey, new ListController<NumberController>(subRegionsOffsets.ConvertAll(i => new NumberController(i))), true);
                        }
                    }
                    RenderRegion(documentController);
                    break;
                case AnnotationType.Ink:
                    break;
                case AnnotationType.Pin:
                    RenderPin(documentController);
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
            this.xItemsControl.ItemsSource = (DataContext as NewAnnotationOverlayViewModel).ViewModels;
        }

        public void LoadPinAnnotations(CustomPdfView pdfView)
        {
            (DataContext as NewAnnotationOverlayViewModel).ViewModels.Clear();
            
            if (pdfView != null)
            {
                var pinAnnotations = _mainDocument.GetDataDocument()
                    .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.PinAnnotationsKey);
                foreach (var doc in pinAnnotations)
                {
                    var dvm = new DocumentViewModel(doc) { Undecorated = true, ResizersVisible = true, DragBounds = new RectangleGeometry { Rect = new Rect(0, 0, pdfView.PdfMaxWidth, pdfView.PdfTotalHeight) } };
                    (DataContext as NewAnnotationOverlayViewModel).ViewModels.Add(dvm);
                }
            }
        }

        private void RegionDocsListOnFieldModelUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs fieldUpdatedEventArgs, Context context)
        {
            if (!(fieldUpdatedEventArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs listArgs)) return;

            switch (listArgs.ListAction)
            {
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                    foreach (DocumentController documentController in listArgs.NewItems)
                    {
	                    var userCreated = documentController.GetDataDocument().GetLinks(KeyStore.LinkToKey)?.TypedData
		                                      .First()?.GetDataDocument()
		                                      .GetField<DocumentController>(KeyStore.LinkDestinationKey, true)
		                                      .GetField<TextController>(KeyStore.LinkContextKey, true)?.Data ==
	                                      nameof(LinkContexts.PushPin);

						if (!userCreated) RenderAnnotation(documentController);
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

        private bool _maskInkUpdates = false;
        private void _inkController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (!_maskInkUpdates)
            {
                XInkCanvas.InkPresenter.StrokeContainer.Clear();
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
            }
        }

        public void SetAnnotationType(AnnotationType type)
        {
            if (type != _currentAnnotationType)
            {
                //ClearPreviewRegion();
                //ClearSelection();
            }
            _currentAnnotationType = type;
            XInkCanvas.InkPresenter.IsInputEnabled = _currentAnnotationType == AnnotationType.Ink;
            XInkCanvas.IsHitTestVisible = _currentAnnotationType == AnnotationType.Ink;
        }

        public DocumentController GetRegionDoc(bool AddToList = true)
        {
            if (_selectedRegion != null)
            {
                return _selectedRegion.RegionDocument;
            }

            DocumentController annotation = null;
            switch (_currentAnnotationType)
            {
                case AnnotationType.Region:
                case AnnotationType.Selection:
                    if (!_regionRectangles.Any(rect => rect.Width > 10 && rect.Height > 10) && (!_currentSelections.Any() || _currentSelections.Last().Key == -1))
                    {
                        ClearSelection(true);
                        goto case AnnotationType.None;
                    }

                    annotation = _regionGetter(_currentAnnotationType);

                    var regionPosList = new ListController<PointController>();
                    var regionSizeList = new ListController<PointController>();
                    var selectionIndexList = new ListController<PointController>();

                    var subRegionsOffsets = new List<double>();
                    double minRegionY = double.PositiveInfinity;
                    foreach (Rect rect in _regionRectangles)
                    {
                        regionPosList.Add(new PointController(rect.X, rect.Y));
                        regionSizeList.Add(new PointController(rect.Width, rect.Height));
                        var pdfView = this.GetFirstAncestorOfType<CustomPdfView>();
                        var imgView = this.GetFirstAncestorOfType<EditableImage>();
                        var scale = pdfView?.ActualWidth / pdfView?.PdfMaxWidth ?? 1;
                        var vOffset = rect.Y * scale; 
                        var scrollRatio = vOffset / pdfView?.TopScrollViewer.ExtentHeight ?? 0;
                        Debug.Assert(!double.IsNaN(scrollRatio));
                        subRegionsOffsets.Add(scrollRatio);
                        minRegionY = Math.Min(rect.Y, minRegionY );
                    }

                    // loop through each selection and add the indices in each selection set
                    var indices = new List<int>();
                    foreach (var selection in _currentSelections)
                    {
                        var ind = _currentSelections.IndexOf(selection);
                        for (var i = selection.Key; i <= selection.Value; i++)
                        {
                            var elem = _textSelectableElements[i];
                            if (_currentSelectionClipRects[ind] == Rect.Empty || _currentSelectionClipRects[ind].Contains(new Point(elem.Bounds.X + elem.Bounds.Width / 2, elem.Bounds.Y + elem.Bounds.Height / 2)))
                            {
                                // this will avoid double selecting any items
                                if (!indices.Contains(i))
                                {
                                    indices.Add(i);
                                }
                            }
                        }
                        selectionIndexList.Add(new PointController(selection.Key, selection.Value));
                    }

                    int prevIndex = -1; 
                    foreach (int index in indices)
                    {
                        SelectableElement elem = _textSelectableElements[index];
                        if (prevIndex + 1 != index)
                        {
                            var pdfView = this.GetFirstAncestorOfType<CustomPdfView>();
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

                    //TODO Add ListController.DeferUpdate

                    annotation.SetField(KeyStore.SelectionRegionTopLeftKey, regionPosList, true);
                    annotation.SetField(KeyStore.SelectionRegionSizeKey, regionSizeList, true);
                    annotation.SetField(KeyStore.SelectionIndicesListKey, selectionIndexList, true);

                    if ((this.GetFirstAncestorOfType<CustomPdfView>()) != null)
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
                case AnnotationType.Pin:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Debug.Assert(annotation != null, "Annotation must be assigned in the switch statement");
            Debug.Assert(!annotation.Equals(_mainDocument),
                "If returning the main document, return it immediately, don't fall through to here");
            annotation.SetRegionDefinition(_mainDocument);
            annotation.SetAnnotationType(_currentAnnotationType);
            RegionDocsList.Add(annotation);
            RegionAdded?.Invoke(this, annotation);

            return annotation;
        }

        public static void LinkRegion(DocumentController sourceDoc, DocumentController targetDoc, double? sStartIndex = null, double? sEndIndex = null, double? tStartIndex = null, double? tEndIndex = null, string linkTag = null)
        {
            Debug.Assert(sourceDoc.GetRegionDefinition() == null);

            DocumentController linkSource = sourceDoc;
            DocumentController linkTarget = targetDoc;

            if (sStartIndex is double sStart && sEndIndex is double sEnd)
            {
                DocumentController sourceRegion = ExistingRegionAtIndices(sourceDoc, sStart, sEnd);

                if (sourceRegion == null)
                {
                    sourceRegion = new RichTextNote().Document;
                    sourceRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionIndicesListKey).Add(new PointController(sStart, sEnd));
                    sourceRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
                    sourceRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);
                    sourceRegion.SetAnnotationType(AnnotationType.Selection);
                    sourceRegion.SetRegionDefinition(sourceDoc);

                    sourceDoc.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey).Add(sourceRegion);
                }

                linkSource = sourceRegion;
            }

            if (tStartIndex is double tStart && tEndIndex is double tEnd)
            {
                DocumentController targetRegion = ExistingRegionAtIndices(sourceDoc, tStart, tEnd);

                if (targetRegion == null)
                {
                    targetRegion = new RichTextNote().Document;
                    targetRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionIndicesListKey).Add(new PointController(tStart, tEnd));
                    targetRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
                    targetRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);
                    targetRegion.SetAnnotationType(AnnotationType.Selection);
                    targetRegion.SetRegionDefinition(targetDoc);

                    targetDoc.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey).Add(targetRegion);
                }

                linkTarget = targetRegion;
            }

            if (linkTag != null) linkSource.Link(linkTarget, LinkContexts.None, linkTag);
            else linkSource.Link(linkTarget, LinkContexts.None);
        }


        //public static void LinkRegion(DocumentController sourceDoc, DocumentController targetDoc, double? sStartIndex = null, double? sEndIndex = null, double? tStartIndex = null, double? tEndIndex = null, string linkTag = null)
        //{
        //    Debug.Assert(sourceDoc.GetRegionDefinition() == null);

        //    var linkSources = new List<DocumentController> { sourceDoc };
        //    var linkTargets = new List<DocumentController> { targetDoc };

        //    if (sStartIndex is double sStart && sEndIndex is double sEnd)
        //    {
        //        RegionWithoutOverlaps(sourceDoc, sStart, sEnd, out var regionsToCreate);
        //        linkSources = regionsToCreate.Where(r => r.Value != null).Select(r => r.Key).ToList();

        //        foreach (var kv in regionsToCreate)
        //        {
        //            DocumentController sourceRegion = kv.Key;

        //            sourceRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionIndicesListKey).Add(kv.Value);
        //            sourceRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
        //            sourceRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);

        //            if (kv.Value != null)
        //            {
        //                sourceRegion.SetRegionDefinition(sourceDoc);
        //                sourceRegion.SetAnnotationType(AnnotationType.Selection);
        //                sourceDoc.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey).Add(sourceRegion);
        //            }
        //        }
        //    }

        //    if (tStartIndex is double tStart && tEndIndex is double tEnd)
        //    {
        //        RegionWithoutOverlaps(targetDoc, tStart, tEnd, out var regionsToCreate);
        //        linkTargets = regionsToCreate.Where(r => r.Value != null).Select(r => r.Key).ToList();

        //        foreach (var kv in regionsToCreate)
        //        {
        //            DocumentController targetRegion = kv.Key;

        //            targetRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionIndicesListKey).Add(kv.Value);
        //            targetRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
        //            targetRegion.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);

        //            if (kv.Value != null)
        //            {
        //                targetRegion.SetRegionDefinition(sourceDoc);
        //                targetRegion.SetAnnotationType(AnnotationType.Selection);
        //                sourceDoc.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey).Add(targetRegion);
        //            }
        //        }
        //    }

        //    foreach (DocumentController target in linkTargets)
        //    {
        //        foreach (DocumentController source in linkSources)
        //        {
        //            if (linkTag != null) source.Link(target, LinkContexts.None, linkTag);
        //            else source.Link(target, LinkContexts.None);
        //        }
        //    }
        //}

        private static DocumentController ExistingRegionAtIndices(DocumentController doc, double startIndex, double endIndex)
        {
            var regions = doc.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);

            if (regions.IsEmpty) return null;

            foreach (DocumentController reg in regions)
            {
                var selectionIndices = reg.GetField<ListController<PointController>>(KeyStore.SelectionIndicesListKey);
                if (selectionIndices.Count > 1) return null;
                PointController selection = selectionIndices[0];
                var start = (int)selection.Data.X;
                var end = (int)selection.Data.Y;
                if ((int)startIndex == start && (int)endIndex == end) return reg;
            }

            return null;
        }

        //private static void RegionWithoutOverlaps(DocumentController doc, double startIndex, double endIndex, out List<KeyValuePair<DocumentController, PointController>> result)
        //{
        //    var regions = doc.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);

        //    if (regions.IsEmpty)
        //    {
        //        result = new List<KeyValuePair<DocumentController, PointController>>
        //        {
        //            new KeyValuePair<DocumentController, PointController>(new RichTextNote().Document, new PointController(startIndex, endIndex))
        //        };
        //        return;
        //    }

        //    foreach (DocumentController reg in regions)
        //    {
        //        var selectionIndices = reg.GetField<ListController<PointController>>(KeyStore.SelectionIndicesListKey);
        //        foreach (PointController range in selectionIndices)
        //        {
        //            var rStart = (int)range.Data.X;
        //            var rEnd = (int)range.Data.Y;

        //            bool contained = startIndex >= rStart && endIndex <= rEnd;

        //            if (contained)
        //            {
        //                result = new List<KeyValuePair<DocumentController, PointController>>
        //                {
        //                    new KeyValuePair<DocumentController, PointController>(reg, null)
        //                };
        //                return;
        //            }

        //            bool isolated = startIndex < rStart && endIndex < rStart || startIndex > rEnd && endIndex > rEnd;

        //            if (isolated)
        //            {
        //                result = new List<KeyValuePair<DocumentController, PointController>>
        //                {
        //                    new KeyValuePair<DocumentController, PointController>(new RichTextNote().Document, new PointController(startIndex, endIndex))
        //                };
        //                return;
        //            }

        //            bool leftOverlap = startIndex < rStart && endIndex >= rStart;
        //            bool rightOverlap = endIndex > rEnd && startIndex <= rEnd;
        //            bool doubleOverlap = leftOverlap && rightOverlap;

        //            Debug.Assert(leftOverlap || rightOverlap);

        //            if (doubleOverlap)
        //            {
        //                result = new List<KeyValuePair<DocumentController, PointController>>
        //                {
        //                    new KeyValuePair<DocumentController, PointController>(new RichTextNote().Document, new PointController(startIndex, rStart)),
        //                    new KeyValuePair<DocumentController, PointController>(reg, null),
        //                    new KeyValuePair<DocumentController, PointController>(new RichTextNote().Document, new PointController(rEnd, endIndex))
        //                };
        //                return;
        //            }

        //            if (leftOverlap)
        //            {
        //                result = new List<KeyValuePair<DocumentController, PointController>>
        //                {
        //                    new KeyValuePair<DocumentController, PointController>(new RichTextNote().Document, new PointController(startIndex, rStart)),
        //                    new KeyValuePair<DocumentController, PointController>(reg, null),
        //                };
        //                return;
        //            }

        //            // if (rightOverlap)
        //            result = new List<KeyValuePair<DocumentController, PointController>>
        //            {
        //                new KeyValuePair<DocumentController, PointController>(reg, null),
        //                new KeyValuePair<DocumentController, PointController>(new RichTextNote().Document, new PointController(rEnd, endIndex))
        //            };
        //            return;
        //        }
        //    }

        //    Debug.Fail("Text selections should be separate from, contained within or one of three forms of overlapping with existing regions. One of the above five cases should be met. If not, maybe Sam messed up!");
        //    result = null;
        //    return;
        //}

        #region General Annotation

        public void StartAnnotation(Point p)
        {
            ClearPreviewRegion();
            //ClearSelection();
            switch (_currentAnnotationType)
            {
                case AnnotationType.Region:
                    StartRegion(p);
                    break;
                case AnnotationType.Selection:
                    StartTextSelection(p);
                    break;
                case AnnotationType.None:
                case AnnotationType.Ink:
                    return;
                case AnnotationType.Pin:
                    CreatePin(p);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UpdateAnnotation(Point p)
        {
            switch (_currentAnnotationType)
            {
                case AnnotationType.Region:
                    UpdateRegion(p);
                    break;
                case AnnotationType.Selection:
                    UpdateTextSelection(p);
                    break;
                case AnnotationType.None:
                case AnnotationType.Ink:
                case AnnotationType.Pin:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void EndAnnotation(Point p)
        {
            DeselectRegion();
            switch (_currentAnnotationType)
            {
                case AnnotationType.Region:
                    EndRegion(p);
                    break;
                case AnnotationType.Selection:
                    EndTextSelection(p);
                    break;
                case AnnotationType.None:
                case AnnotationType.Ink:
                    return;
                case AnnotationType.Pin:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void TappedAnnotation(Point p)
        {
            //TODO Popup annotation
        }

        #endregion

        #region Ink Annotation

        private void InkPresenterOnStrokesErased(InkPresenter inkPresenter, InkStrokesErasedEventArgs inkStrokesErasedEventArgs)
        {
            _maskInkUpdates = true;
            _inkController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            _maskInkUpdates = false;
        }

        private void InkPresenter_StrokesCollected(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesCollectedEventArgs args)
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

        private bool _annotatingRegion = false;
        private Point _previewStartPoint;
        private List<Rect> _regionRectangles = new List<Rect>();
        public void StartRegion(Point p)
        {
            if (_currentAnnotationType != AnnotationType.Region)
            {
                return;
            }

            if (!this.IsCtrlPressed())
            {
                if (_regionRectangles.Any() || _currentSelections.Any())
                {
                    ClearSelection();
                }
            }
            _annotatingRegion = true;
            _previewStartPoint = p;
            Canvas.SetLeft(XPreviewRect, p.X);
            Canvas.SetTop(XPreviewRect, p.Y);
            XPreviewRect.Width = 0;
            XPreviewRect.Height = 0;
            XPreviewRect.Visibility = Visibility.Visible;
            if (!XAnnotationCanvas.Children.Contains(XPreviewRect))
            {
                XAnnotationCanvas.Children.Insert(0, XPreviewRect);
            }
            _regionRectangles.Add(new Rect(p.X, p.Y, 0, 0));
        }

        public DocumentController MakeAnnotationPinDoc(Point point, DocumentController linkedDoc = null)
        {
			var annotation = _regionGetter(AnnotationType.Pin);
		    annotation.SetPosition(new Point(point.X + 10, point.Y + 10));
		    annotation.SetWidth(10);
		    annotation.SetHeight(10);
		    annotation.GetDataDocument().SetField(KeyStore.RegionTypeKey, new TextController(nameof(AnnotationType.Pin)), true);
            if (linkedDoc != null)
            {
                annotation.Link(linkedDoc, LinkContexts.PushPin);
            }

            RegionDocsList.Add(annotation);
		    RegionAdded?.Invoke(this, annotation);
            //format pin annotation
            return annotation;
        }

        /// <summary>
        /// Call this method if you just want to make a pushpin annotation with the default text.
        /// </summary>
        /// <param name="point"></param>
        private async void CreatePin(Point point)
        {
            if (_currentAnnotationType != AnnotationType.Pin && _currentAnnotationType != AnnotationType.Region)
            {
                return;
            }
            
            foreach (var region in XAnnotationCanvas.Children)
            {
                if (region is Ellipse existingPin && existingPin.GetBoundingRect(this).Contains(point))
                {
                    return;
                }
            }
/*
			var richText = new RichTextNote("<annotation>", new Point(point.X + 10, point.Y + 10),
		        new Size(150, 75));
	        richText.Document.SetField(KeyStore.BackgroundColorKey, new TextController(Colors.White.ToString()), true);
	        var annotation = MakeAnnotationPinDoc(point, richText.Document);
	        richText.Document.SetHidden(true); // hidden flag will be toggled off when annotation is rendered after annotation is added to RegionDocsList-- why??
	        //set pos & region type
			annotation.Link(richText.Document, LinkContexts.PushPin);

			var pdfView = this.GetFirstAncestorOfType<CustomPdfView>();
            var scale = pdfView.Width / pdfView.PdfMaxWidth;

            var dvm = new DocumentViewModel(richText.Document) { Undecorated = true, ResizersVisible = true,
                   DragBounds = new RectangleGeometry { Rect = new Rect(0, 0, pdfView.PdfMaxWidth, pdfView.PdfTotalHeight) } };
            (DataContext as NewAnnotationOverlayViewModel).ViewModels.Add(dvm);

            // bcz: should this be called in LoadPinAnnotations as well?
             dvm.DocumentController.AddFieldUpdatedListener(KeyStore.GoToRegionLinkKey,
                delegate(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
                {
                    if (args.NewValue != null)
                    {
                        var regionDef = (args.NewValue as DocumentController).GetDataDocument()
                            .GetField<DocumentController>(KeyStore.LinkDestinationKey).GetDataDocument().GetRegionDefinition();
                        var pos = regionDef.GetPosition().Value;
                        pdfView.ScrollToPosition(pos.Y);
                        dvm.DocumentController.RemoveField(KeyStore.GoToRegionLinkKey);
                    }
                });
            _mainDocument.GetDataDocument()
                .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.PinAnnotationsKey)
                .Add(dvm.DocumentController);
				*/

	        DocumentController annotationController;

	        var pdfView = this.GetFirstAncestorOfType<CustomPdfView>();
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
			/*
			var annotation = _regionGetter(AnnotationType.Pin);
		    annotation.SetPosition(new Point(point.X + 10, point.Y + 10));
		    annotation.SetWidth(10);
		    annotation.SetHeight(10);
		    annotation.GetDataDocument().SetField(KeyStore.RegionTypeKey, new TextController(nameof(AnnotationType.Pin)), true);
		    annotation.Link(target, LinkContexts.PushPin);
		    RegionDocsList.Add(annotation);
		    RegionAdded?.Invoke(this, annotation);
		    RenderPin(annotation, target);
			*/
			var annotation = MakeAnnotationPinDoc(point, target);
            RenderPin(annotation, target);

            var pdfView = this.GetFirstAncestorOfType<CustomPdfView>();
			var width = pdfView?.PdfMaxWidth ??
			            this.GetFirstAncestorOfType<DocumentView>().ActualWidth;
			var height= pdfView?.PdfTotalHeight ??
			            this.GetFirstAncestorOfType<DocumentView>().ActualHeight;

			var dvm = new DocumentViewModel(target)
		    {
			    Undecorated = true,
			    ResizersVisible = true,
			    DragBounds = new RectangleGeometry { Rect = new Rect(0, 0, width, height) }
		    };
		    (DataContext as NewAnnotationOverlayViewModel).ViewModels.Add(dvm);

		    // bcz: should this be called in LoadPinAnnotations as well?
		    dvm.DocumentController.AddFieldUpdatedListener(KeyStore.GoToRegionLinkKey,
			    delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
			    {
				    if (args.NewValue != null)
				    {
					    var regionDef = (args.NewValue as DocumentController).GetDataDocument()
						    .GetField<DocumentController>(KeyStore.LinkDestinationKey).GetDataDocument().GetRegionDefinition();
					    var pos = regionDef.GetPosition().Value;
					    pdfView?.ScrollToPosition(pos.Y);
					    dvm.DocumentController.RemoveField(KeyStore.GoToRegionLinkKey);
				    }
			    });
		    _mainDocument.GetDataDocument()
			    .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.PinAnnotationsKey)
			    .Add(dvm.DocumentController);
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

		    videoNote.SetField(KeyStore.LinkContextKey, new TextController(nameof(LinkContexts.PushPin)), true);
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
		    imageNote.SetField(KeyStore.LinkContextKey, new TextController(nameof(LinkContexts.PushPin)), true);
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
			richText.Document.SetField(KeyStore.LinkContextKey, new TextController(nameof(LinkContexts.PushPin)), true);

		    return richText.Document;
		}

        private void RenderPin(DocumentController region, DocumentController dest = null)
        {
            var point = region.GetPosition() ?? new Point(0, 0);
            point.X -= 10;
            point.Y -= 10;
            var pin = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.OrangeRed),
                IsDoubleTapEnabled = false
            };
            Canvas.SetLeft(pin, point.X - pin.Width / 2);
            Canvas.SetTop(pin, point.Y - pin.Height / 2);
            XAnnotationCanvas.Children.Add(pin);

            var vm = new SelectionViewModel(region, new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), new SolidColorBrush(Colors.OrangeRed));
            pin.DataContext = vm;

			var tip = new ToolTip()
			{
				Placement = PlacementMode.Bottom,
			};
			ToolTipService.SetToolTip(pin, tip);

            pin.PointerExited += (s, e) => tip.IsOpen = false;
	        pin.PointerEntered += (s, e) =>
            {
                tip.IsOpen = true;
                //update tag content based on current tags of region
                var tags = new ObservableCollection<string>();
				ListController<DocumentController> linksFrom = region.GetDataDocument().GetLinks(KeyStore.LinkFromKey);

				if (linksFrom != null)
				{
					foreach (var link in linksFrom)
					{
						var currtags = link.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey).Data;
						foreach (TextController text in currtags)
						{
							tags.Add(text.Data);
						}
					}
				}

				ListController<DocumentController> linksTo = region.GetDataDocument().GetLinks(KeyStore.LinkToKey);

				if (linksTo != null)
				{
					foreach (var link in linksTo)
					{
						var currtags = link.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey)?.Data;
						if (currtags != null)
						{
							foreach (TextController text in currtags)
							{
								tags.Add(text.Data);
							}
						}
						
					}
				}

		        var content = tags.Count == 0 ? "" : tags[0];
		        if (tags.Count > 0) tags.Remove(tags[0]);
		        foreach (var str in tags)
		        {
			        content = content + ", " + str;
		        }
		        tip.Content = content;
	        };
			pin.Tapped += (sender, args) =>
            {
                if (this.IsCtrlPressed() && this.IsAltPressed())
                {
                    (DataContext as NewAnnotationOverlayViewModel).ViewModels.Remove(pin.DataContext as DocumentViewModel);
                    var docView = _pinAnnotations.FirstOrDefault(i => i.ViewModel.DocumentController.Equals(dest));
                    if (docView != null)
                    {
                        if (XAnnotationCanvas.Children.Contains(docView)) XAnnotationCanvas.Children.Remove(docView);
                        _pinAnnotations.Remove(docView);
                        _mainDocument.GetDataDocument()
                            .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.PinAnnotationsKey)
                            .Remove(docView.ViewModel.DocumentController);
                    }
                }
                SelectRegion(vm, args.GetPosition(this));
                args.Handled = true;
            };

            pin.PointerPressed += (s, e) => e.Handled = true;

            //handlers for moving pin
            pin.ManipulationMode = ManipulationModes.All;
	        pin.ManipulationStarted += (s, e) =>
	        {
		        pin.ManipulationMode = ManipulationModes.All;
		        e.Handled = true;
	        };
	        pin.ManipulationDelta += (s, e) =>
	        {
		        var p = Util.DeltaTransformFromVisual(e.Delta.Translation, s as UIElement);
		        Canvas.SetLeft(pin, Canvas.GetLeft(pin) + p.X);
		        Canvas.SetTop(pin, Canvas.GetTop(pin) + p.Y);
		        e.Handled = true;
	        };

	        FormatRegionOptionsFlyout(region, pin);
			
			//formatting bindings
			pin.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath(nameof(vm.SelectionColor)),
                Mode = BindingMode.OneWay
            });
            pin.SetBinding(VisibilityProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(AnnotationVisibility)),
                Converter = new BoolToVisibilityConverter(),
                Mode = BindingMode.OneWay
            });

            _regions.Add(vm);
            
        }

	    private void FormatRegionOptionsFlyout(DocumentController region, UIElement regionGraphic)
	    {
		    // context menu that toggles whether annotations should be show/ hidden on scroll

		    MenuFlyout flyout = new MenuFlyout();
		    MenuFlyoutItem visOnScrollON = new MenuFlyoutItem();
		    MenuFlyoutItem visOnScrollOFF = new MenuFlyoutItem();
		    visOnScrollON.Text = "Annotations Visibile On Scroll : TURN ON";
		    visOnScrollOFF.Text = "Annotations Visibile On Scroll : TURN OFF";
		    visOnScrollON.Click += (sender, args) => { region.Tag = PinAnnotationVisibility.VisibleOnScroll; };
		    visOnScrollOFF.Click += (sender, args) => { region.Tag = PinAnnotationVisibility.ManualToggle; };
		    regionGraphic.ContextFlyout = flyout;
		    regionGraphic.RightTapped += (s, e) =>
		    {
			    var currVisibileOnScroll = region.Tag?.Equals(PinAnnotationVisibility.VisibleOnScroll) ?? false;
			    var item = currVisibileOnScroll ? visOnScrollOFF : visOnScrollON;
			    flyout.Items.Clear();
			    flyout.Items.Add(item);
			    flyout.ShowAt(regionGraphic as FrameworkElement);
		    };
		}

        public void UpdateRegion(Point p)
        {
            if (_currentAnnotationType != AnnotationType.Region)
            {
                return;
            }

            if (!_annotatingRegion)
            {
                return;
            }

            if (p.X < _previewStartPoint.X)
            {
                XPreviewRect.Width = _previewStartPoint.X - p.X;
                Canvas.SetLeft(XPreviewRect, p.X);
            }
            else
            {
                XPreviewRect.Width = p.X - _previewStartPoint.X;
            }

            if (p.Y < _previewStartPoint.Y)
            {
                XPreviewRect.Height = _previewStartPoint.Y - p.Y;
                Canvas.SetTop(XPreviewRect, p.Y);
            }
            else
            {
                XPreviewRect.Height = p.Y - _previewStartPoint.Y;
            }
            XPreviewRect.Visibility = Visibility.Visible;
        }

        public void EndRegion(Point p)
        {
            if (_currentAnnotationType != AnnotationType.Region)
            {
                return;
            }


            _annotatingRegion = false;

            if (_regionRectangles.Count > 0)
            {
                _regionRectangles[_regionRectangles.Count - 1] =
                    new Rect(Canvas.GetLeft(XPreviewRect), Canvas.GetTop(XPreviewRect), XPreviewRect.Width,
                        XPreviewRect.Height);

                if (_regionRectangles.Last().Width < 4 || _regionRectangles.Last().Height < 4)
                {
                    _regionRectangles.RemoveAt(_regionRectangles.Count - 1);
                    return;
                }
            }

            var viewRect = new Rectangle
            {
                Width = XPreviewRect.Width,
                Height = XPreviewRect.Height,
                Fill = XPreviewRect.Fill,
                Opacity = XPreviewRect.Opacity
            };
            XAnnotationCanvas.Children.Add(viewRect);
            Canvas.SetLeft(viewRect, Canvas.GetLeft(XPreviewRect));
            Canvas.SetTop(viewRect, Canvas.GetTop(XPreviewRect));
        }

        public void RenderNewRegion(DocumentController region)
        {
            var r = new RegionAnnotation(region);
            r.Tapped += (sender, args) =>
            {
                SelectRegion(sender as ISelectable, args.GetPosition(this));
                args.Handled = true;
            };
            r.Visibility = Visibility.Visible;
            r.Background = new SolidColorBrush(Colors.Goldenrod);
            Canvas.SetTop(r, region.GetPosition().Value.Y);
			
			FormatRegionOptionsFlyout(region, r);
            //r.SetBinding(VisibilityProperty, new Binding
            //{
            //    Source = this,
            //    Path = new PropertyPath(nameof(AnnotationVisibility)),
            //    Converter = new BoolToVisibilityConverter()
            //});
            XAnnotationCanvas.Children.Add(r);
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

            public SolidColorBrush SelectedBrush { get; set; } = new SolidColorBrush(Color.FromArgb(60, 0, 255, 0));

            public SolidColorBrush UnselectedBrush { get; set; } = new SolidColorBrush(Color.FromArgb(128, 255, 255, 0));

            public SelectionViewModel(DocumentController region, 
                SolidColorBrush selectedBrush= null, 
                SolidColorBrush unselectedBrush= null)
            {
                RegionDocument = region;
                UnselectedBrush = unselectedBrush;
                SelectedBrush = selectedBrush;
                _selectionColor = UnselectedBrush;
            }

            public bool Selected { get; private set; } = false;

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

        public List<SelectableElement> _textSelectableElements;

        public void SetSelectableElements(IEnumerable<SelectableElement> selectableElements)
        {
            _textSelectableElements = selectableElements.ToList();

            foreach (var documentController in RegionDocsList)
            {
                RenderAnnotation(documentController);
            }
        }

        public void ClearSelection(bool hardReset = false)
        {
            _currentSelections.Clear();
            _currentSelectionClipRects.Clear();
            _selectionStartPoint = hardReset ? null : _selectionStartPoint;
            _selectedRectangles.Clear();
            XSelectionCanvas.Children.Clear();
            XPreviewRect.Width = XPreviewRect.Height = 0;
            _regionRectangles.Clear();
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

        public void StartTextSelection(Point p)
        {
            if (!this.IsCtrlPressed())
            {
                if (_currentSelections.Any() || _regionRectangles.Any())
                {
                    ClearSelection();
                }
            }
           // _currentSelections.Add(new KeyValuePair<int, int>(-1, -1));
            _selectionStartPoint = p;
        }

        public void UpdateTextSelection(Point p)
        {
            if (_selectionStartPoint.HasValue)
            {
                if (Math.Abs(_selectionStartPoint.Value.X - p.X) < 3 &&
                    Math.Abs(_selectionStartPoint.Value.Y - p.Y) < 3)
                {
                    return;
                }
                var dir = new Point(p.X - _selectionStartPoint.Value.X, p.Y - _selectionStartPoint.Value.Y);
                var startEle = GetClosestElementInDirection(_selectionStartPoint.Value, dir);
                if (startEle == null)
                {
                    return;
                }
                var currentEle = GetClosestElementInDirection(p, new Point(-dir.X, -dir.Y));
                if (currentEle == null)
                {
                    return;
                }
                SelectElements(Math.Min(startEle.Index, currentEle.Index), Math.Max(startEle.Index, currentEle.Index), _selectionStartPoint ?? new Point(), p);
            }
        }

        public void EndTextSelection(Point p)
        {
            if (!_currentSelections.Any() || _currentSelections.Last().Key == -1) return;//Not currently selecting anything
            _selectionStartPoint = null;
        }

        private void RenderRegion(DocumentController region)
        {
            var posList = region.GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
            var sizeList = region.GetField<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);
            var indexList = region.GetField<ListController<PointController>>(KeyStore.SelectionIndicesListKey);

            Debug.Assert(posList.Count == sizeList.Count);

            var vm = new SelectionViewModel(region, new SolidColorBrush(Color.FromArgb(0x30, 0xff, 0, 0)), new SolidColorBrush(Color.FromArgb(100, 0xff, 0xff, 0)));

            for (var i = 0; i < posList.Count; ++i)
            {
                RenderSubRegion(posList[i].Data, sizeList[i].Data, vm);
            }

            if (_textSelectableElements != null)
            {
                foreach (PointController t in indexList)
                {
                    Point range = t.Data;
                    for (var ind = (int)range.X; ind <= (int)range.Y; ind++)
                    {
                        Rect rect = _textSelectableElements[ind].Bounds;
                        RenderSubRegion(new Point(rect.X, rect.Y), new Point(rect.Width, rect.Height), vm);
                    }
                }
            }

            _regions.Add(vm);
        }

        private void RenderSubRegion(Point pos, Point size, SelectionViewModel vm)
        {
            var r = new Rectangle
            {
                Width = size.X,
                Height = size.Y,
                Fill = vm.UnselectedBrush,
                DataContext = vm,
                IsDoubleTapEnabled = false
            };
            r.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath(nameof(vm.SelectionColor)),
                Mode = BindingMode.OneWay
            });
            Canvas.SetLeft(r, pos.X);
            Canvas.SetTop(r, pos.Y);
            r.Tapped += (sender, args) =>
            {
                if (this.IsCtrlPressed() && this.IsAltPressed())
                {
                    XAnnotationCanvas.Children.Remove(r);
                }
                SelectRegion(vm, args.GetPosition(this));
                args.Handled = true;
            };
			//TOOLTIP TO SHOW TAGS
	        var tip = new ToolTip()
	        {
		        Placement = PlacementMode.Bottom,
	        };
	        ToolTipService.SetToolTip(r, tip);
            r.PointerExited += (s, e) => tip.IsOpen = false;
            r.PointerEntered += (s, e) =>
	        {
                tip.IsOpen = true;
		        //update tag content based on current tags of region
		        var tags = new ObservableCollection<string>();
		        ListController<DocumentController> linksFrom = vm.RegionDocument.GetDataDocument().GetLinks(KeyStore.LinkFromKey);

		        if (linksFrom != null)
		        {
			        foreach (var link in linksFrom)
			        {
				        var currtags = link.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey).Data;
				        foreach (TextController text in currtags)
				        {
					        tags.Add(text.Data);
				        }
			        }
		        }

		        ListController<DocumentController> linksTo = vm.RegionDocument.GetDataDocument().GetLinks(KeyStore.LinkToKey);

		        if (linksTo != null)
		        {
			        foreach (var link in linksTo)
			        {
				        var currtags = link.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey)?.Data;
				        if (currtags != null)
				        {
					        foreach (TextController text in currtags)
					        {
						        tags.Add(text.Data);
					        }
				        }

			        }
		        }

		        var content = tags.Count == 0 ? null : tags[0];
		        if (tags.Count > 0) tags.Remove(tags[0]);
		        foreach (var str in tags)
		        {
			        content = content + ", " + str;
		        }
		        tip.Content = content;
	        };
			r.SetBinding(VisibilityProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(AnnotationVisibility)),
                Converter = new BoolToVisibilityConverter()
            });

			FormatRegionOptionsFlyout(vm.RegionDocument, r);
            XAnnotationCanvas.Children.Add(r);
        }

        #region Selection Logic

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
            foreach (var selectableElement in _textSelectableElements)
            {
                var b = selectableElement.Bounds;
                if (b.Contains(p) && !string.IsNullOrWhiteSpace(selectableElement.Contents as string))
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

        private void DeselectIndex(int index, Rect? clipRect = null)
        {
            if (_selectedRectangles.ContainsKey(index))
            {
                var ele = _textSelectableElements[index];
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
            var ele = _textSelectableElements[index];
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
        private Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();

        private void SelectElements(int startIndex, int endIndex, Point start, Point end)
        {
            // if control isn't pressed, reset the selection
            if (this.IsAltPressed())
            {
                var bounds = new Rect(new Point(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y)),
                             new Point(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y)));
                foreach (var ele in _textSelectableElements)
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
            if (!_currentSelections.Any() || !_currentSelections.Any(sel => sel.Key <= startIndex && startIndex <= sel.Value))
            {
                // create a new selection
                _currentSelections.Add(new KeyValuePair<int, int>(-1, -1));
                _currentSelectionClipRects.Add(Rect.Empty);
            }
            var currentSelectionStart = _currentSelections.Last().Key;
            var currentSelectionEnd   = _currentSelections.Last().Value;
            var lastSelectionClipRect = _currentSelectionClipRects.LastOrDefault();

            _currentSelectionClipRects[_currentSelectionClipRects.Count - 1] = this.IsAltPressed() ? 
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
                    SelectIndex(i, _currentSelectionClipRects.LastOrDefault());
                }
            }
            else
            {
                if (currentSelectionStart == -1 || (lastSelectionClipRect != null && lastSelectionClipRect != Rect.Empty))
                {
                    for (var i = startIndex; i <= endIndex; ++i)
                    {
                        SelectIndex(i, _currentSelectionClipRects.LastOrDefault());
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
            _currentSelections[_currentSelections.Count - 1] = new KeyValuePair<int, int>(startIndex, endIndex);
        }

        #endregion

        #endregion

        public LinkHandledResult HandleLink(DocumentController linkDoc, LinkDirection direction)
        {
            if ((linkDoc.GetDataDocument().GetField<TextController>(KeyStore.LinkContextKey)?.Data
                     .Equals(nameof(LinkContexts.PushPin)) ?? false) &&
                RegionDocsList.Contains(linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey)))
            {
                var dest = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey);
                dest.ToggleHidden();

                return LinkHandledResult.HandledClose;
            }

            return LinkHandledResult.Unhandled;
        }
        private List<DocumentView> _pinAnnotations = new List<DocumentView>();

	    public void OnDragEnter(object sender, DragEventArgs e)
	    {
		    var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
		    if (dragModel != null && dragModel.DraggedDocument != null && dragModel.DraggedKey == null)
		    {
		        e.AcceptedOperation |= DataPackageOperation.Copy;
		    }
		    else
		    {
			    e.AcceptedOperation = DataPackageOperation.None;
		    }
		    //e.Handled = true;
	    }

	    public void OnDrop(object sender, DragEventArgs e)
	    {
	        if (!this.IsShiftPressed())
	        {
	            return;
	        }
			var dragModel = (DragDocumentModel) e.DataView.Properties[nameof(DragDocumentModel)];
		    var where = e.GetPosition(XAnnotationCanvas);
		    var target = dragModel.GetDropDocument(where);
		    if (!target.DocumentType.Type.Equals("Rich Text Box") && !target.DocumentType.Type.Equals("Text Box"))
		    {
			    if (target.GetActualSize()?.X > 200)
			    {
					var ratio = target.GetHeight() / target.GetWidth();
					target.SetField(KeyStore.WidthFieldKey, new NumberController(200), true);
					target.SetField(KeyStore.HeightFieldKey, new NumberController(200 * ratio), true);
				}
			}
		    CreatePin(where, target);
		    e.Handled = true;
	    }

	    
    }

}
