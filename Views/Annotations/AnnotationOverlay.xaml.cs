using Dash.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using NewControls.Geometry;
using static Dash.DataTransferTypeInfo;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public partial class AnnotationOverlay : UserControl, ILinkHandler, INotifyPropertyChanged
    {
        readonly InkController                  _inkController;
        AnnotationType                          _currAnnotationType = AnnotationType.None;
        ObservableCollection<DocumentViewModel> _embeddedViewModels = new ObservableCollection<DocumentViewModel>();
        bool                                    _maskInkUpdates;
        [CanBeNull] AnchorableAnnotation        _currentAnnotation;

        public readonly DocumentController     MainDocument;
        public readonly RegionGetter           GetRegion;
        public readonly AnnotationManager      AnnotationManager;
        public AnchorableAnnotation.Selection  SelectedRegion;
        public List<SelectableElement>         TextSelectableElements;
        public List<AnchorableAnnotation>      CurrentAnchorableAnnotations = new List<AnchorableAnnotation>();
        public delegate DocumentController     RegionGetter(AnnotationType type);
        public readonly ListController<DocumentController> RegionDocsList; // shortcut to the region documents stored in the RegionsKey
        public readonly ListController<DocumentController> EmbeddedDocsList; // shortcut to the embedded documents stored in the EmbeddedDocs Key
        public IEnumerable<AnchorableAnnotation.Selection> SelectableRegions => XAnnotationCanvas.Children.OfType<AnchorableAnnotation>().Where((a) => a.ViewModel != null).Select((a) => a.ViewModel);
        public AnnotationType                 CurrentAnnotationType
        {
            get =>_currAnnotationType;
            set
            {
                _currAnnotationType = value;
                OnPropertyChanged();

                //XInkCanvas.InkPresenter.IsInputEnabled = _currAnnotationType == AnnotationType.Ink;
                //XInkCanvas.IsHitTestVisible = _currAnnotationType == AnnotationType.Ink;
            }
        }

        public List<int> PageInformation { get; set; }

        public AnnotationOverlay([NotNull] DocumentController viewDocument, [NotNull] RegionGetter getRegion)
        {
            InitializeComponent();

            MainDocument = viewDocument;
            GetRegion = getRegion;

            AnnotationManager = new AnnotationManager(this);

            RegionDocsList   = MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);
            EmbeddedDocsList = MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.EmbeddedDocumentsKey);
            _inkController   = MainDocument.GetDataDocument().GetFieldOrCreateDefault<InkController>(KeyStore.InkDataKey);

            //XInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            //XInkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            //XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            //XInkCanvas.InkPresenter.IsInputEnabled = false;
            //XInkCanvas.IsHitTestVisible = false;
            //XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
            Loaded += onLoaded;
            Unloaded += onUnloaded;
            
            var binding = new Binding()
            {
                Source = this,
                Path = new PropertyPath(nameof(CurrentAnnotationType)),
                Mode = BindingMode.TwoWay,
                Converter = new CursorConverter()
            };
           // this.SetBinding(Mouse.CursorProperty, binding);

        }
        public class CursorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                switch ((AnnotationType)value) {
                    case AnnotationType.Selection: return CoreCursorType.IBeam;
                    case AnnotationType.Region: return CoreCursorType.Cross;
                }
                return CoreCursorType.Arrow;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }

        public void SelectRegion(DocumentController region)
        {
            var documentView = this.GetFirstAncestorOfType<DocumentView>();
            documentView.Visibility = Visibility.Visible;

            var deselect = SelectedRegion?.IsSelected == true;
            var selectable = SelectableRegions.FirstOrDefault(sel => sel.RegionDocument.Equals(region));
            foreach (var nvo in documentView.GetDescendantsOfType<AnnotationOverlay>())
                foreach (var r in nvo.SelectableRegions.Where(r => r.RegionDocument.Equals(selectable?.RegionDocument)))
                {
                    if (nvo.SelectedRegion != null)
                        nvo.SelectedRegion.IsSelected = false;
                    nvo.SelectedRegion = deselect ? null : r;
                    if (!deselect) {
                        r.IsSelected = true;
                    }
                    if (documentView.ViewModel != null)
                    {
                        documentView.ViewModel.SearchHighlightState = deselect ? DocumentViewModel.UnHighlighted : DocumentViewModel.Highlighted;
                    }
                }
        }
        public void DeselectRegion()
        {
            var documentView = this.GetFirstAncestorOfType<DocumentView>();
            var selectedRegion = SelectedRegion;
            if (selectedRegion != null)
                foreach (var nvo in documentView.GetDescendantsOfType<AnnotationOverlay>())
                    foreach (var r in nvo.SelectableRegions.Where(r => r.RegionDocument.Equals(selectedRegion.RegionDocument)))
                    {
                        nvo.SelectedRegion.IsSelected = false;
                        nvo.SelectedRegion = null;
                    }
        }
        /// <summary>
        /// Creates a region document from a preview, or returns an already selected region
        /// </summary>
        /// <returns></returns>
        public DocumentController CreateRegionFromPreviewOrSelection()
        {
            var annotation = SelectedRegion?.RegionDocument;
            if (annotation == null &&
                (CurrentAnnotationType == AnnotationType.Region || CurrentAnnotationType == AnnotationType.Selection))
            {
                if (CurrentAnchorableAnnotations.Any() &&
                    !CurrentAnchorableAnnotations.OfType<RegionAnnotation>().Any(i => i?.Width < 10 && i?.Height < 10))
                {
                    annotation = GetRegion(CurrentAnnotationType);

                    var subRegionsOffsets = CurrentAnchorableAnnotations.Select((item) => item.AddToRegion(annotation)).ToList();
                    subRegionsOffsets.Sort((y1, y2) => Math.Sign(y1 - y2));

                    annotation.GetDataDocument().SetPosition(new Point(0, subRegionsOffsets.FirstOrDefault()));
                    annotation.SetRegionDefinition(MainDocument);
                    annotation.SetAnnotationType(CurrentAnnotationType);
                    RegionDocsList.Add(annotation); // this actually adds the region to the parent document's Regions list
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

        void onUnloaded(object o, RoutedEventArgs routedEventArgs)
        {
            RegionDocsList.FieldModelUpdated -= regionDocsListOnFieldModelUpdated;
            _inkController.FieldModelUpdated -= inkController_FieldModelUpdated;
        }
        void onLoaded(object o, RoutedEventArgs routedEventArgs)
        {
            _inkController.FieldModelUpdated += inkController_FieldModelUpdated;
            RegionDocsList.FieldModelUpdated += regionDocsListOnFieldModelUpdated;
            EmbeddedDocsList.FieldModelUpdated += embeddedDocsListOnFieldModelUpdated;
            xItemsControl.ItemsSource = _embeddedViewModels;
            embeddedDocsListOnFieldModelUpdated(null, 
                new ListController<DocumentController>.ListFieldUpdatedEventArgs(ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add, EmbeddedDocsList.TypedData, new List<DocumentController>(),0), null);
           _embeddedViewModels.Clear();

            RegionDocsList.ToList().ForEach((reg) => XAnnotationCanvas.Children.Add(reg.CreateAnnotationAnchor(this)));
            EmbeddedDocsList.ToList().ForEach((doc) =>
                _embeddedViewModels.Add(new DocumentViewModel(doc)
                {
                    Undecorated = true,
                    ResizersVisible = true,
                    DragWithinParentBounds = true
                }));
        }

        void embeddedDocsListOnFieldModelUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs args, Context c)
        {
            if ((args is ListController<DocumentController>.ListFieldUpdatedEventArgs listArgs) &&
                 listArgs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add)
                listArgs.NewItems.ForEach((reg) => _embeddedViewModels.Add(
                    new DocumentViewModel(reg)
                    {
                        Undecorated = true,
                        ResizersVisible = true,
                        DragWithinParentBounds = true
                    }));
        }
        void regionDocsListOnFieldModelUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs args, Context c)
        {
            if (args is ListController<DocumentController>.ListFieldUpdatedEventArgs listArgs)
            {
                switch (listArgs.ListAction)
                {
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                    listArgs.NewItems.ForEach((reg) => XAnnotationCanvas.Children.Add(reg.CreateAnnotationAnchor(this)));
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
                    XAnnotationCanvas.Children.OfType<RegionAnnotation>().ToList().ForEach((reg) =>
                    {
                        if (listArgs.OldItems.Contains(reg.RegionDocumentController))
                            XAnnotationCanvas.Children.Remove(reg);
                    });
                    break;
                }
            }
        }
        void inkController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (!_maskInkUpdates)
            {
                //XInkCanvas.InkPresenter.StrokeContainer.Clear();
                //XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        public async void EmbedDocumentWithPin(Point point, DocumentController embeddedDocument = null)
        {
            _currentAnnotation = XAnnotationCanvas.Children.OfType<PinAnnotation>().Where((pin) =>
            {

                var rect = pin.GetBoundingRect(this);
                rect.X -= pin.ActualWidth;
                rect.Y -= pin.ActualHeight;
                rect.Width = pin.ActualWidth * 2;
                rect.Height = pin.ActualHeight * 2;
                return rect.Contains(point);
            }).FirstOrDefault();
            if (_currentAnnotation == null)
            {
                embeddedDocument = embeddedDocument ?? await createEmbeddedTextNote(this, point);
                EmbeddedDocsList.Add(embeddedDocument);
                StartAnnotation(AnnotationType.Pin, point, new AnchorableAnnotation.Selection(CreatePinRegion(point, embeddedDocument)));
            }
        }

        public void StartAnnotation(AnnotationType type, Point p, AnchorableAnnotation.Selection svm = null)
        {
            XPreviewRect.Visibility = Visibility.Collapsed;
            switch (type)
            {
                case AnnotationType.Pin:       _currentAnnotation = new PinAnnotation(this, svm); break;
                case AnnotationType.Region:    _currentAnnotation = new RegionAnnotation(this, svm); break;
                case AnnotationType.Selection: _currentAnnotation = new TextAnnotation(this, svm); break;
            }
            if (!this.IsCtrlPressed() && CurrentAnchorableAnnotations.Any())
            {
                ClearSelection();
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
        static async Task<DocumentController> createEmbeddedTextNote(AnnotationOverlay parent, Point where)
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
        #endregion

        #region Ink Annotation

        private void InkPresenterOnStrokesErased(InkPresenter inkPresenter, InkStrokesErasedEventArgs inkStrokesErasedEventArgs)
        {
            //_maskInkUpdates = true;
            //_inkController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            //_maskInkUpdates = false;
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            //_maskInkUpdates = true;
            //_inkController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            //_maskInkUpdates = false;
        }

        #endregion

        #region Selection Annotation

        readonly Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();

        public void ClearSelection(bool hardReset = false)
        {
            CurrentAnchorableAnnotations.Clear();
            _selectedRectangles.Clear();
            XSelectionCanvas.Children.Clear();
            XPreviewRect.Width = XPreviewRect.Height = 0;
            _clipRectSelections.Clear();
            var removeItems = XAnnotationCanvas.Children.Where(i => !((i as FrameworkElement)?.DataContext is AnchorableAnnotation.Selection) && i != XPreviewRect).ToList();
            if (XAnnotationCanvas.Children.Any())
            {
                var lastAdded = XAnnotationCanvas.Children.Last();
                if (!((lastAdded as FrameworkElement)?.DataContext is AnchorableAnnotation.Selection))
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


        private void DeselectIndex(int index, Rect? clipRect = null, int endIndex = -1)
        {
            if (_selectedRectangles.ContainsKey(index))
            {
                var ele = TextSelectableElements[index];
                if (clipRect == null || clipRect == Rect.Empty || clipRect?.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2,
                        ele.Bounds.Y + ele.Bounds.Height / 2)) == true)
                {                    var currRect = _selectedRectangles[index];
                    var left = Canvas.GetLeft(currRect);
                    var right = Canvas.GetLeft(currRect) + currRect.Width;
                    if (endIndex != -1)
                    {
                        {
                            // if we're deselecting text backwards
                            if (ele.Bounds.Left - left < ele.Bounds.Width)
                            {
                                // if we've reached a different line
                                if (Math.Abs(Canvas.GetTop(currRect) -
                                             TextSelectableElements[index + 1].Bounds.Top) > TextSelectableElements[index].Bounds.Height / 2 ||
                                    Math.Abs(Canvas.GetLeft(currRect) - TextSelectableElements[index + 1].Bounds.Left) > TextSelectableElements[index].Bounds.Width * 4)
                                {
                                    currRect.Visibility = Visibility.Collapsed;
                                    XAnnotationCanvas.Children.Remove(currRect);
                                } else
                                {
                                    Canvas.SetLeft(currRect, TextSelectableElements[index + 1].Bounds.Left);
                                    currRect.Width =
                                        Math.Max(right - TextSelectableElements[index + 1].Bounds.Left, 0);
                                }
                            }
                            // if we're deselecting text forwards
                            else if (ele.Bounds.Right - right < ele.Bounds.Width)
                            {
                                // if we've reached a different line
                                if (Math.Abs(Canvas.GetTop(currRect) -
                                             TextSelectableElements[index - 1].Bounds.Top) > TextSelectableElements[index].Bounds.Height / 2)
                                {
                                    currRect.Visibility = Visibility.Collapsed;
                                    XAnnotationCanvas.Children.Remove(currRect);
                                } else
                                {
                                    currRect.Width =
                                        Math.Max(TextSelectableElements[index - 1].Bounds.Right - left, 0);
                                }
                            }
                        }
                    }
                    _selectedRectangles.Remove(index);

                }
            }
        }

        private Rectangle _currRect;

        private void SelectIndex(int index, Rect? clipRect = null)
        {
            var ele = TextSelectableElements[index];
            if (clipRect == null || clipRect == Rect.Empty ||
                clipRect?.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2,
                    ele.Bounds.Y + ele.Bounds.Height / 2)) == true)
            {
                if (_selectedRectangles.ContainsKey(index))
                {
                    _currRect = _selectedRectangles[index];
                }
            }

            if (_currRect == null)
            {
                _currRect = new Rectangle
                {
                    Width = ele.Bounds.Width,
                    Height = ele.Bounds.Height,
                    Fill = new SolidColorBrush(Color.FromArgb(120, 0x94, 0xA5, 0xBB))
                };
                Canvas.SetLeft(_currRect, ele.Bounds.Left);
                Canvas.SetTop(_currRect, ele.Bounds.Top);
                XSelectionCanvas.Children.Add(_currRect);
            }

            var closeEnough = Math.Abs(ele.Bounds.Left - (Canvas.GetLeft(_currRect) + _currRect.Width)) <
                              ele.Bounds.Width * 4 && Math.Abs(ele.Bounds.Top - Canvas.GetTop(_currRect)) <
                              ele.Bounds.Height / 2;
            var similarSize = ele.Bounds.Height - _currRect.Height < ele.Bounds.Height;
            if (closeEnough && similarSize)
            {
                var left = Canvas.GetLeft(_currRect);
                var right = Canvas.GetLeft(_currRect) + _currRect.Width;
                if (ele.Bounds.Left < left)
                {
                    Canvas.SetLeft(_currRect, ele.Bounds.Left);
                    _currRect.Width = right - ele.Bounds.Left;
                }
                else
                {
                    _currRect.Width = Math.Max(_currRect.Width, ele.Bounds.Right - left);
                }
                _currRect.Height = Math.Max(_currRect.Height, ele.Bounds.Bottom - Canvas.GetTop(_currRect));
            }
            else
            {
                if (_currRect.IsInVisualTree() && _currRect.GetBoundingRect(this).Contains(ele.Bounds))
                {
                    _selectedRectangles[index] = _currRect;
                    return;
                }

                _currRect = new Rectangle
                {
                    Width = ele.Bounds.Width,
                    Height = ele.Bounds.Height,
                    Fill = new SolidColorBrush(Color.FromArgb(120, 0x94, 0xA5, 0xBB))
                };
                Canvas.SetLeft(_currRect, ele.Bounds.Left);
                Canvas.SetTop(_currRect, ele.Bounds.Top);
                XSelectionCanvas.Children.Add(_currRect);
            }

            _selectedRectangles[index] = _currRect;
        }

        public void SelectElements(int startIndex, int endIndex, Point start, Point end) 
        {
            if (_currentAnnotation is TextAnnotation textAnnotation)
            {
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
                    SelectFromClipRect(currentClipRect);
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
                            DeselectIndex(i, null, startIndex - 1);
                        }

                        for (var i = currentSelectionEnd + 1; i <= endIndex; ++i)
                        {
                            SelectIndex(i);
                        }

                        for (var i = endIndex + 1; i <= currentSelectionEnd; ++i)
                        {
                            DeselectIndex(i, null, currentSelectionEnd);
                        }
                    }
                }

                textAnnotation.StartIndex = startIndex;
                textAnnotation.EndIndex = endIndex;
            }
        }

        private List<Rectangle> _clipRectSelections = new List<Rectangle>();

        private void SelectFromClipRect(Rect currentClipRect)
        {
            var rectsToRemove = new List<Rectangle>();
            foreach (var rect in _clipRectSelections)
            {
                var belowTopBound = Canvas.GetTop(rect) + rect.Height > currentClipRect.Top;
                var belowBottomBound = Canvas.GetTop(rect) < currentClipRect.Bottom;
                if (!(belowTopBound && belowBottomBound))
                {
                    rectsToRemove.Add(rect);
                }
            }

            rectsToRemove.ForEach(r =>
            {
                _clipRectSelections.Remove(r);
                r.Visibility = Visibility.Collapsed;
                XSelectionCanvas.Children.Remove(r);
                var keys = new List<int>();
                foreach (var key in _selectedRectangles.Where(kvp => kvp.Value.Equals(r)).Select(kvp => kvp.Key))
                {
                    keys.Add(key);
                }

                foreach (var key in keys)
                {
                    _selectedRectangles.Remove(key);
                }
            });

            var startPage = GetPageOf(currentClipRect.Top);
            var endPage = GetPageOf(currentClipRect.Bottom);
            var startIndex = PageInformation[startPage - 1] + 1;
            var endIndex = PageInformation[endPage];
            
            //Debug.WriteLine(_clipRectSelections.Count);
            for (var index = startIndex; index <= endIndex; index++)
            {
                var ele = TextSelectableElements[index];
                if (currentClipRect.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2,
                        ele.Bounds.Y + ele.Bounds.Height / 2))
                    /*currentClipRect.Contains(new Point(ele.Bounds.Left, ele.Bounds.Top)) ||
                    currentClipRect.Contains(new Point(ele.Bounds.Right, ele.Bounds.Bottom))*/)
                {
                    var found = false;
                    foreach (var rect in _clipRectSelections)
                    {
                        if (found) break;
                        //var ahhh = ele.Bounds.Left < Canvas.GetLeft(rect) + rect.Width + ele.Bounds.Width && Canvas.GetLeft(rect) < ele.Bounds.Left && ele.Bounds.Left - rect.Width < Canvas.GetLeft(rect) + ele.Bounds.Width;
                        //var fromRight = Canvas.GetLeft(rect) - ele.Bounds.Left < ele.Bounds.Width * 2 && Canvas.GetLeft(rect) > ele.Bounds.Left && Canvas.GetLeft(rect) - ele.Bounds.Right < ele.Bounds.Width;
                        //var closeEnough = (ahhh || fromRight) &&
                        //                  Math.Abs(ele.Bounds.Top - Canvas.GetTop(rect)) <
                        //                  ele.Bounds.Height / 2;
                        var rLeft = Canvas.GetLeft(rect);
                        var rTop = Canvas.GetTop(rect);
                        var closeEnough = Math.Abs(ele.Bounds.Left - rLeft) <
                                          ele.Bounds.Width + rect.Width &&
                                          Math.Abs(ele.Bounds.Top - rTop) <
                                          ele.Bounds.Height / 5;
                        var similarSize = ele.Bounds.Height - rect.Height < ele.Bounds.Height;
                        if (closeEnough && similarSize)
                        {
                            Canvas.SetLeft(rect, Math.Min(rLeft, ele.Bounds.Left));
                            rect.Width = Math.Max(rect.Width, ele.Bounds.Right - rLeft);
                            Canvas.SetTop(rect, Math.Min(rTop, ele.Bounds.Top));
                            rect.Height = Math.Abs(ele.Bounds.Bottom - rTop);
                            _selectedRectangles[ele.Index] = rect;
                            found = true;
                        }
                        else if (new Rect(rLeft, rTop, rect.Width, rect.Height).Contains(ele.Bounds))
                        {
                            found = true;
                            _selectedRectangles[ele.Index] = rect;
                        }
                    }

                    if (!found && !_selectedRectangles.ContainsKey(ele.Index))
                    {
                        var newRect = new Rectangle
                        {
                            Width = ele.Bounds.Width,
                            Height = ele.Bounds.Height,
                            Fill = new SolidColorBrush(Color.FromArgb(120, 0x94, 0xA5, 0xBB))
                        };
                        Canvas.SetLeft(newRect, ele.Bounds.Left);
                        Canvas.SetTop(newRect, ele.Bounds.Top);
                        XSelectionCanvas.Children.Add(newRect);
                        _clipRectSelections.Add(newRect);
                        _selectedRectangles[ele.Index] = newRect;
                    }
                }
                else if (_selectedRectangles.ContainsKey(ele.Index))
                {
                    foreach (var rect in _clipRectSelections)
                    {
                        var rbounds = new Rect(Canvas.GetLeft(rect), Canvas.GetTop(rect), rect.Width, rect.Height);
                        if ( // rect.IsInVisualTree() &&
                            (rbounds.Contains(new Point(ele.Bounds.Left, ele.Bounds.Top)) ||
                             rbounds.Contains(new Point(ele.Bounds.Right, ele.Bounds.Bottom))))
                        {
                            if (ele.Bounds.Left - rbounds.Left > ele.Bounds.Width)
                            {
                                rect.Width = ele.Bounds.Left - rbounds.Left;
                            }
                            else
                            {
                                rect.Width = Math.Abs(rbounds.Left + rect.Width - ele.Bounds.Right);
                                Canvas.SetLeft(rect, ele.Bounds.Right);
                            }

                            _selectedRectangles.Remove(ele.Index);
                        }
                    }
                }
            }
        }

        private int GetPageOf(double yOffset)
        {
            var pages = this.GetFirstAncestorOfType<PdfView>().BottomPages.PageSizes;
            var currOffset = 0.0;
            var i = 0;
            do
            {
                currOffset += pages[i].Height;
            } while (currOffset < yOffset && ++i < pages.Count);

            return i;
        }

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

        CoreCursor IBeam = new CoreCursor(CoreCursorType.IBeam, 1);
        CoreCursor Cross = new CoreCursor(CoreCursorType.Cross, 1);
        private void LayoutRoot_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {

            if (!this.IsLeftBtnPressed() && !this.IsRightBtnPressed())
            {
                Window.Current.CoreWindow.PointerCursor = CurrentAnnotationType == AnnotationType.Region ? Cross : IBeam;

                e.Handled = true;
            }
        }
    }
}
