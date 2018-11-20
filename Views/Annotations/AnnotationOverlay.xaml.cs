using Dash.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public partial class AnnotationOverlay : UserControl, ILinkHandler, INotifyPropertyChanged
    {
        private InkController _inkController;
        private AnnotationType _currAnnotationType = AnnotationType.None;
        private bool _maskInkUpdates = false;
        [CanBeNull] private AnchorableAnnotation _currentAnnotation;


        public delegate DocumentController RegionGetter(AnnotationType type);
        public readonly DocumentController MainDocument;
        public readonly RegionGetter GetRegion;
        public readonly AnnotationManager AnnotationManager;
        public AnchorableAnnotation.Selection SelectedRegion;
        public List<SelectableElement> TextSelectableElements;
        public List<AnchorableAnnotation> CurrentAnchorableAnnotations = new List<AnchorableAnnotation>();
        public ListController<DocumentController> RegionDocsList; // shortcut to the region documents stored in the RegionsKey
        public ListController<DocumentController> EmbeddedDocsList => AnnotationOverlayEmbeddings.EmbeddedDocsList; // shortcut to the embedded documents stored in the EmbeddedDocs Key
        public IEnumerable<AnchorableAnnotation.Selection> SelectableRegions => XAnnotationCanvas.Children.OfType<AnchorableAnnotation>().Select((a) => a.ViewModel).Where((a) => a != null);
        public AnnotationType CurrentAnnotationType
        {
            get => _currAnnotationType;
            set
            {
                _currAnnotationType = value;
                OnPropertyChanged();

                if (XInkCanvas != null)
                {
                    XInkCanvas.InkPresenter.IsInputEnabled = _currAnnotationType == AnnotationType.Ink;
                    XInkCanvas.IsHitTestVisible = _currAnnotationType == AnnotationType.Ink;
                }
            }
        }

        public List<int> PageEndIndices { get; set; }

        private InkCanvas XInkCanvas { get; }

        public AnnotationOverlayEmbeddings AnnotationOverlayEmbeddings { get; set; }
        public AnnotationOverlay([NotNull] DocumentController viewDocument, [NotNull] RegionGetter getRegion)
        {
            InitializeComponent();

            MainDocument = viewDocument;
            GetRegion = getRegion;

            AnnotationManager = new AnnotationManager(this);
            AnnotationOverlayEmbeddings = new AnnotationOverlayEmbeddings(this);

            if (MainPage.Instance.xSettingsView.UseInkCanvas)
            {
                XInkCanvas = new InkCanvas();
                XInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
                XInkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
                XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
                XInkCanvas.InkPresenter.IsInputEnabled = false;
                XInkCanvas.IsHitTestVisible = false;
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
            }

            RegionDocsList = MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);
            _inkController = MainDocument.GetDataDocument().GetFieldOrCreateDefault<InkController>(KeyStore.InkDataKey);
            MainDocument.GetDataDocument().AddWeakFieldUpdatedListener(this, KeyStore.InkDataKey, (view, controller, arge) => view.inkController_FieldModelUpdated(controller, arge));
            MainDocument.GetDataDocument().AddWeakFieldUpdatedListener(this, KeyStore.RegionsKey, (view, controller, arge) => view.regionDocsListOnFieldModelUpdated(controller, arge));
            regionDocsListOnFieldModelUpdated(null,
                new DocumentController.DocumentFieldUpdatedEventArgs(null, null, DocumentController.FieldUpdatedAction.Update, null,
              new ListController<DocumentController>.ListFieldUpdatedEventArgs(ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add, RegionDocsList.ToList(), new List<DocumentController>(), 0), false));
        }

        public class CursorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                switch ((AnnotationType)value)
                {
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
            if (documentView == null)
                return;
            documentView.Visibility = Visibility.Visible;

            var deselect = SelectedRegion?.IsSelected == true;
            var selectable = SelectableRegions.FirstOrDefault(sel => sel.RegionDocument.Equals(region));
            foreach (var nvo in documentView.GetDescendantsOfType<AnnotationOverlay>())
                foreach (var r in nvo.SelectableRegions.Where(r => r.RegionDocument.Equals(selectable?.RegionDocument)))
                {
                    if (nvo.SelectedRegion != null)
                        nvo.SelectedRegion.IsSelected = false;
                    nvo.SelectedRegion = deselect ? null : r;
                    if (!deselect)
                    {
                        r.IsSelected = true;
                    }

                    documentView.ViewModel?.SetHighlight(!deselect);
                }
        }
        public void DeselectRegion()
        {
            var documentView = this.GetFirstAncestorOfType<DocumentView>();
            var selectedRegion = SelectedRegion;
            if (selectedRegion != null)
            {
                foreach (var nvo in documentView.GetDescendantsOfType<AnnotationOverlay>())
                {
                    foreach (var r in nvo.SelectableRegions.Where(r => r.RegionDocument.Equals(selectedRegion.RegionDocument)))
                    {
                        if (nvo.SelectedRegion != null)
                        {
                            nvo.SelectedRegion.IsSelected = false;
                            nvo.SelectedRegion = null;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Creates a region document from a preview, or returns an already selected region
        /// </summary>
        /// <returns></returns>
        public async Task<DocumentController> CreateRegionFromPreviewOrSelection(DocumentController linkedDoc = null)
        {
            var annotation = SelectedRegion?.RegionDocument;
            if (annotation == null &&
                (CurrentAnnotationType == AnnotationType.Region || CurrentAnnotationType == AnnotationType.Selection))
            {
                if (CurrentAnchorableAnnotations.Any() &&
                    !CurrentAnchorableAnnotations.OfType<RegionAnnotation>().Any(i => i?.Width < 10 && i?.Height < 10))
                {
                    var rtb = new RenderTargetBitmap();
                    var pdfview = this.GetFirstAncestorOfType<PdfView>().GetFirstAncestorOfType<DocumentView>();
                    await rtb.RenderAsync(pdfview, (int)pdfview.ActualWidth, (int)pdfview.ActualHeight);

                    var buf = (await rtb.GetPixelsAsync()).ToArray();
                    var bitmap = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
                    bitmap.PixelBuffer.AsStream().Write(buf, 0, buf.Length);

                    var util = new ImageToDashUtil();
                    var imgnote = await util.ParseBitmapAsync(bitmap);

                    annotation = imgnote;
                    //annotation = GetRegion(CurrentAnnotationType);

                    var subRegionsOffsets = CurrentAnchorableAnnotations.Select((item) => item.AddToRegion(annotation)).ToList();
                    subRegionsOffsets.Sort((y1, y2) => Math.Sign(y1 - y2));

                    annotation.GetDataDocument().SetPosition(new Point(0, subRegionsOffsets.FirstOrDefault()));
                    annotation.SetRegionDefinition(MainDocument);
                    annotation.SetAnnotationType(CurrentAnnotationType);
                    if (linkedDoc != null)
                    {
                        annotation.Link(linkedDoc, LinkBehavior.Overlay, null);
                    }
                    RegionDocsList.Add(annotation); // this actually adds the region to the parent document's Regions list

                    /*var text =
                        DateTime.Now.ToString("g") + " | Created an annotation using pdf: " +
                        pdfview.ViewModel.DocumentController.Title;
                    var eventDoc = new RichTextNote(text).Document;
                    var tags = "pdf, annotation, " + pdfview.ViewModel.DocumentController.Title;
                    eventDoc.GetDataDocument().SetField<TextController>(KeyStore.EventTagsKey, tags, true);
                    eventDoc.GetDataDocument().SetField(KeyStore.EventCollectionKey,
                        pdfview.ParentCollection.ViewModel.ContainerDocument, true);
                    eventDoc.Link(annotation, LinkBehavior.Annotate);
                    eventDoc.SetField(KeyStore.EventDisplay1Key, annotation, true);
                    var displayXaml =
                        @"<Grid
                            xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                            xmlns:dash=""using:Dash""
                            xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">
                            <Grid.RowDefinitions>
                                <RowDefinition Height=""Auto""></RowDefinition>
                                <RowDefinition Height=""*""></RowDefinition>
                                <RowDefinition Height=""*""></RowDefinition>
                            </Grid.RowDefinitions>
                            <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Background=""White"">
                                <TextBlock x:Name=""xTextFieldData"" HorizontalAlignment=""Stretch"" Height=""Auto"" VerticalAlignment=""Top""/>
                            </Border>
                            <StackPanel Orientation=""Horizontal"" Grid.Row=""2"">
                                <dash:DocumentView x:Name=""xDocumentField_EventDisplay1Key""
                                    Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""2""
                                    VerticalAlignment=""Top"" />
                            </StackPanel>
                            </Grid>";
                    EventManager.EventOccured(eventDoc, displayXaml);*/
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
            annotation.AddToListField(KeyStore.SelectionRegionTopLeftKey, new PointController(point.X, point.Y));
            annotation.AddToListField(KeyStore.SelectionRegionSizeKey, new PointController(1, 1));
            if (linkedDoc != null)
            {
                annotation.Link(linkedDoc, LinkBehavior.Overlay, null);
            }

            //var pdfview = this.GetFirstAncestorOfType<PdfView>().GetFirstAncestorOfType<DocumentView>();
            //var text = "Created a pin annotation using pdf: " + pdfview.ViewModel.DocumentController.Title;

            //if (linkedDoc != null)
            //{
            //    text += "\n " + linkedDoc.GetField<TextController>(KeyStore.DataKey)?.Data;
            //}

            //var eventdoc = new RichTextNote(text).Document;
            //var tags = "pdf, annotation, pin, " + pdfview.ViewModel.DocumentController.Title;
            //eventdoc.GetDataDocument().SetField<TextController>(KeyStore.EventTagsKey, tags, true);
            //eventdoc.GetDataDocument().SetField(KeyStore.EventCollectionKey,
            //    pdfview.ParentCollection.ViewModel.ContainerDocument, true);
            //eventdoc.Link(annotation, LinkBehavior.Overlay);
            //eventdoc.SetField(KeyStore.EventDisplay1Key, annotation, true);
            //var displayXaml =
            //    @"<Grid
            //                xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
            //                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
            //                xmlns:dash=""using:Dash""
            //                xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">
            //                <Grid.RowDefinitions>
            //                    <RowDefinition Height=""Auto""></RowDefinition>
            //                    <RowDefinition Height=""*""></RowDefinition>
            //                    <RowDefinition Height=""*""></RowDefinition>
            //                </Grid.RowDefinitions>
            //                <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Background=""White"">
            //                    <TextBlock x:Name=""xTextFieldData"" HorizontalAlignment=""Stretch"" Height=""Auto"" VerticalAlignment=""Top""/>
            //                </Border>
            //                <StackPanel Orientation=""Horizontal"" Grid.Row=""2"">
            //                    <dash:DocumentView x:Name=""xDocumentField_EventDisplay1Key""
            //                        Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""2""
            //                        VerticalAlignment=""Top"" />
            //                </StackPanel>
            //                </Grid>";
            //EventManager.EventOccured(eventdoc, displayXaml);

            RegionDocsList.Add(annotation);
            return annotation;
        }


        private void regionDocsListOnFieldModelUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs args)
        {
            if (args is DocumentController.DocumentFieldUpdatedEventArgs dargs && dargs.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs listArgs)
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

        private void inkController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args)
        {
            if (!_maskInkUpdates && XInkCanvas != null)
            {
                XInkCanvas.InkPresenter.StrokeContainer.Clear();
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
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
            var linkSource = sStartIndex is double sStart && sEndIndex is double sEnd
                ? createRegionDoc(sourceDoc, sStart, sEnd)
                : sourceDoc;
            var linkTarget = tStartIndex is double tStart && tEndIndex is double tEnd
                ? createRegionDoc(targetDoc, tStart, tEnd)
                : targetDoc;

            linkSource.Link(linkTarget, LinkBehavior.Follow, linkTag);

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
                    var selInds = reg.GetField<ListController<PointController>>(KeyStore.SelectionIndicesListKey);
                    return (selInds.Count == 1 && ((int)startIndex == (int)selInds[0].Data.X &&
                                                   (int)endIndex == (int)selInds[0].Data.Y));
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
            if (XPreviewRect.GetBoundingRect(this).Contains(point))
            {
                embeddedDocument = embeddedDocument ?? await createEmbeddedTextNote(this, point);
                EmbeddedDocsList.Add(embeddedDocument);
                StartAnnotation(AnnotationType.Region, point, new AnchorableAnnotation.Selection(await CreateRegionFromPreviewOrSelection(embeddedDocument)));
                return;
            }
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
            case AnnotationType.Pin: _currentAnnotation = new PinAnnotation(this, svm); break;
            case AnnotationType.Region: _currentAnnotation = new RegionAnnotation(this, svm); break;
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
            if (XInkCanvas != null)
            {
                _maskInkUpdates = true;
                _inkController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                _maskInkUpdates = false;
            }
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            if (XInkCanvas != null)
            {
                _maskInkUpdates = true;
                _inkController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                _maskInkUpdates = false;
            }
        }

        #endregion

        #region Selection Annotation

        private readonly Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();

        public void ClearSelection(bool hardReset = false)
        {
            CurrentAnchorableAnnotations.Clear();
            _selectedRectangles.Clear();
            XSelectionCanvas.Children.Clear();
            XPreviewRect.Width = XPreviewRect.Height = 0;
            _clipRectSelections.Clear();
            var removeItems = XAnnotationCanvas.Children.Where(i => !((i as FrameworkElement)?.DataContext is AnchorableAnnotation.Selection)).ToList();
            if (XAnnotationCanvas.Children.Any())
            {
                var lastAdded = XAnnotationCanvas.Children.Last();
                if (!((lastAdded as FrameworkElement)?.DataContext is AnchorableAnnotation.Selection))
                {
                    removeItems.Add(lastAdded);
                }
            }
            foreach (var item in removeItems.Where((i) => i != XPreviewRect))
            {
                XAnnotationCanvas.Children.Remove(item);
            }
        }

        /// <summary>
        ///     Deselects the index passed. If endIndex is passed in as a parameter, and the rectangle
        ///     selected by the index isn't the same as the rectangle of the endIndex, it will just remove
        ///     the index's rectangle and all references to it (because we're going to be deselecting all
        ///     of the indices in between anyways).
        /// </summary>
        /// <param name="index"></param>
        /// <param name="clipRect"></param>
        /// <param name="endIndex"></param>
        private void DeselectIndex(int index, Rect? clipRect = null, int endIndex = -1)
        {
            if (_selectedRectangles.ContainsKey(index))
            {
                // if we've already removed the rectangle, we don't need to do the math, just remove the index
                if (_selectedRectangles[index].Visibility == Visibility.Collapsed)
                {
                    _selectedRectangles.Remove(index);
                    return;
                }

                var ele = TextSelectableElements[index];
                var clipRectNonexistent = clipRect == null || clipRect == Rect.Empty;
                var clipRectContainsIndex = clipRect?.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2,
                                                ele.Bounds.Y + ele.Bounds.Height / 2)) == true;
                if (clipRectNonexistent || clipRectContainsIndex)
                {
                    var currRect = _selectedRectangles[index];
                    var left = Canvas.GetLeft(currRect);
                    var right = Canvas.GetLeft(currRect) + currRect.Width;
                    var top = Canvas.GetTop(currRect);
                    if (endIndex != -1)
                    {
                        // if we're deselecting text backwards
                        if (ele.Bounds.Left - left < ele.Bounds.Width)
                        {
                            var farEnoughY = Math.Abs(top -
                                                      TextSelectableElements[index + 1].Bounds.Top) >
                                             TextSelectableElements[index].Bounds.Height / 5;
                            var farEnoughX = Math.Abs(left - TextSelectableElements[index + 1].Bounds.Left) >
                                             TextSelectableElements[index].Bounds.Width * 4;
                            // if we've reached a different line
                            if (farEnoughY || farEnoughX)
                            {
                                // deselect the whole rectangle
                                currRect.Visibility = Visibility.Collapsed;
                                XAnnotationCanvas.Children.Remove(currRect);
                            }
                            else
                            {
                                Canvas.SetLeft(currRect, TextSelectableElements[index + 1].Bounds.Left);
                                currRect.Width =
                                    Math.Max(right - TextSelectableElements[index + 1].Bounds.Left, 0);
                            }
                        }
                        // if we're deselecting text forwards
                        else if (ele.Bounds.Right - right < ele.Bounds.Width)
                        {
                            var farEnoughY = Math.Abs(top -
                                                      TextSelectableElements[index - 1].Bounds.Top) >
                                             TextSelectableElements[index].Bounds.Height / 5;
                            // if we've reached a different line
                            if (farEnoughY)
                            {
                                // deselect the whole rectangle
                                currRect.Visibility = Visibility.Collapsed;
                                XAnnotationCanvas.Children.Remove(currRect);
                            }
                            else
                            {
                                currRect.Width =
                                    Math.Max(TextSelectableElements[index - 1].Bounds.Right - left, 0);
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
            var clipRectNonexistent = clipRect == null || clipRect == Rect.Empty;
            var clipRectContainsIndex = clipRect?.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2,
                                            ele.Bounds.Y + ele.Bounds.Height / 2)) == true;
            if (clipRectNonexistent || clipRectContainsIndex)
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

            var left = Canvas.GetLeft(_currRect);
            var right = Canvas.GetLeft(_currRect) + _currRect.Width;
            var top = Canvas.GetTop(_currRect);
            var closeEnoughX = Math.Abs(ele.Bounds.Left - right) <
                               ele.Bounds.Width * 4;
            var closeEnoughY = Math.Abs(ele.Bounds.Top - top) <
                              ele.Bounds.Height / 5;
            var similarSize = ele.Bounds.Height - _currRect.Height < ele.Bounds.Height;
            // if we should just adjust the current rectangle
            if (closeEnoughX && closeEnoughY && similarSize)
            {
                // if selecting backwards
                if (ele.Bounds.Left < left)
                {
                    Canvas.SetLeft(_currRect, ele.Bounds.Left);
                    _currRect.Width = right - ele.Bounds.Left;
                }
                // if selecting forwards
                else
                {
                    _currRect.Width = Math.Max(_currRect.Width, ele.Bounds.Right - left);
                }
                _currRect.Height = Math.Max(_currRect.Height, ele.Bounds.Bottom - top);
            }
            // if we should make a new rectangle
            else
            {
                // double check that the current rectangle doesn't contain the new one we would make
                if (new Rect(left, top, _currRect.Width, _currRect.Height).Contains(ele.Bounds))
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
                    if (currentSelectionStart == -1 || currentClipRect != null && currentClipRect != Rect.Empty)
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
            // for each rectangle, if it's not between the current clip rectangle, we should remove it
            foreach (var rect in _clipRectSelections)
            {
                var rTop = (rect.RenderTransform as TranslateTransform).Y;
                var belowTopBound = rTop + rect.Height > currentClipRect.Top;
                var belowBottomBound = rTop < currentClipRect.Bottom;
                if (!(belowTopBound && belowBottomBound))
                {
                    rectsToRemove.Add(rect);
                }
            }

            rectsToRemove.ForEach(r =>
            {
                // remove the rectangle
                _clipRectSelections.Remove(r);
                r.Visibility = Visibility.Collapsed;
                XSelectionCanvas.Children.Remove(r);
                var keys = new List<int>();
                // remove every key that points to the rectangle
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
            // startIndex is either 0 or the last page's end index + 1
            var startIndex = startPage > 0 ? PageEndIndices[startPage - 1] + 1 : 0;
            var endIndex = PageEndIndices[endPage];

            // loop through the indices between the possible pages
            for (var index = startIndex; index <= endIndex; index++)
            {
                var ele = TextSelectableElements[index];
                if (currentClipRect.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2,
                        ele.Bounds.Y + ele.Bounds.Height / 2)))
                {
                    var found = false;
                    foreach (var rect in _clipRectSelections)
                    {
                        var rLeft = (rect.RenderTransform as TranslateTransform).X;
                        var rTop = (rect.RenderTransform as TranslateTransform).Y;
                        var closeEnoughX = Math.Abs(ele.Bounds.Left - rLeft) < ele.Bounds.Width + rect.Width;
                        var closeEnoughY = Math.Abs(ele.Bounds.Top - rTop) < ele.Bounds.Height / 5;
                        var similarSize = ele.Bounds.Height - rect.Height < ele.Bounds.Height;

                        // if the element is close enough to append to the rectangle
                        if (closeEnoughX && closeEnoughY && similarSize)
                        {
                            (rect.RenderTransform as TranslateTransform).X = Math.Min(rLeft, ele.Bounds.Left);
                            (rect.RenderTransform as TranslateTransform).Y = Math.Min(rTop, ele.Bounds.Top);
                            //Canvas.SetLeft(rect, Math.Min(rLeft, ele.Bounds.Left));
                            rect.Width = Math.Max(rect.Width, ele.Bounds.Right - rLeft);
                            //Canvas.SetTop(rect, Math.Min(rTop, ele.Bounds.Top));
                            rect.Height = Math.Abs(ele.Bounds.Bottom - rTop);
                            _selectedRectangles[ele.Index] = rect;
                            found = true;
                            break;
                        }
                        // if the element is in the rectangle
                        else if (new Rect(rLeft, rTop, rect.Width, rect.Height).Contains(ele.Bounds))
                        {
                            found = true;
                            _selectedRectangles[ele.Index] = rect;
                            break;
                        }
                    }

                    // if we still haven't found a rectangle for the element
                    if (!found && !_selectedRectangles.ContainsKey(ele.Index))
                    {
                        // create a new rectangle
                        var newRect = new Rectangle
                        {
                            Width = ele.Bounds.Width,
                            Height = ele.Bounds.Height,
                            Fill = new SolidColorBrush(Color.FromArgb(120, 0x94, 0xA5, 0xBB)),
                            RenderTransform = new TranslateTransform { X = ele.Bounds.Left, Y = ele.Bounds.Top }
                        };
                        //Canvas.SetLeft(newRect, ele.Bounds.Left);
                        //Canvas.SetTop(newRect, ele.Bounds.Top);
                        XSelectionCanvas.Children.Add(newRect);
                        _clipRectSelections.Add(newRect);
                        _selectedRectangles[ele.Index] = newRect;
                    }
                }
                else if (_selectedRectangles.ContainsKey(ele.Index))
                {
                    foreach (var rect in _clipRectSelections)
                    {
                        var rbounds = new Rect((rect.RenderTransform as TranslateTransform).X, (rect.RenderTransform as TranslateTransform).Y, rect.Width, rect.Height);
                        if (rbounds.Contains(new Point(ele.Bounds.Left, ele.Bounds.Top)) ||
                            rbounds.Contains(new Point(ele.Bounds.Right, ele.Bounds.Bottom)))
                        {
                            if (ele.Bounds.Left - rbounds.Left > ele.Bounds.Width)
                            {
                                rect.Width = ele.Bounds.Left - rbounds.Left;
                            }
                            else
                            {
                                rect.Width = Math.Abs(rbounds.Left + rect.Width - ele.Bounds.Right);
                                (rect.RenderTransform as TranslateTransform).X = ele.Bounds.Right;
                            }

                            _selectedRectangles.Remove(ele.Index);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Returns the page of the PDF given a y-offset relative to the PDF
        /// </summary>
        /// <param name="yOffset"></param>
        /// <returns></returns>
        public int GetPageOf(double yOffset)
        {
            var pages = this.GetFirstAncestorOfType<PdfView>().DefaultView.Pages.PageSizes;
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
            if (linkDoc.GetDataDocument().GetLinkBehavior() == LinkBehavior.Overlay &&
                RegionDocsList.Contains(linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey)))
            {
                var dest = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey);
                var val = this.GetDescendantsOfType<DocumentView>().FirstOrDefault(dv => dv.ViewModel.DataDocument.Equals(dest.GetDataDocument()));
                if (val != null)
                {
                    val.ViewModel.LayoutDocument.ToggleHidden();
                    return LinkHandledResult.HandledClose;
                }
            }

            return LinkHandledResult.Unhandled;
        }

        public void OnDragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = e.DataView.HasDragModel() ? e.AcceptedOperation | DataPackageOperation.Copy : DataPackageOperation.None;
        }

        public async void OnDrop(object sender, DragEventArgs e)
        {
            var where = e.GetPosition(XAnnotationCanvas);
            if (e.DataView.HasDataOfType(Internal) && !this.IsAltPressed())
            {
                var dm = e.DataView.GetDragModel() as DragDocumentModel;
                if (!this.IsShiftPressed() && dm?.DraggingLinkButton == false)
                {
                    if (dm == null)
                        return;
                    // if docs are being moved within the overlay, then they will be placed appropriately and returned from this call.
                    // if docs are being dragged onto this overlay, we disallow that and no droppedDocs are returned from this call.
                    var droppedDocs = await e.DataView.GetDroppableDocumentsForDataOfType(Any, sender as FrameworkElement, where);
                    e.AcceptedOperation = droppedDocs.Count > 0 ? DataPackageOperation.Move : DataPackageOperation.None;
                    e.Handled = true;
                    if (e.AcceptedOperation == DataPackageOperation.Move && !MainPage.Instance.IsShiftPressed() && !MainPage.Instance.IsAltPressed() && !MainPage.Instance.IsCtrlPressed())
                    {
                        for (var i = 0; i < dm.DraggedDocCollectionViews?.Count; i++)
                        {
                            if (!AnnotationOverlayEmbeddings.GetDescendants().Contains(dm.DraggedDocumentViews[i]))
                            {
                                EmbeddedDocsList.Add(droppedDocs.FirstOrDefault());
                            }
                            if (dm.DraggedDocumentViews != null)
                            {
                                MainPage.Instance.ClearFloaty(dm.DraggedDocumentViews[i]);
                            }

                            if (dm.DraggedDocCollectionViews[i] == null)
                            {
                                var overlayEmbeddings = dm.DraggedDocumentViews[i]?.GetFirstAncestorOfType<AnnotationOverlayEmbeddings>();
                                if (AnnotationOverlayEmbeddings != overlayEmbeddings)
                                {
                                    overlayEmbeddings?.EmbeddedDocsList.Remove(dm.DraggedDocuments[i]);
                                }
                            }
                            else
                            {
                                dm.DraggedDocCollectionViews[i].RemoveDocument(dm.DraggedDocuments[i]);
                            }
                        }
                    }
                }
                else if (dm?.DraggingLinkButton == true && !this.IsShiftPressed())
                {
                    var targets = await e.DataView.GetDroppableDocumentsForDataOfType(Internal, sender as FrameworkElement, where, true);
                    StartAnnotation(AnnotationType.Pin, where, new AnchorableAnnotation.Selection(CreatePinRegion(where, targets.FirstOrDefault())));
                    e.Handled = true;
                }
                else
                {
                    var targets = await e.DataView.GetDroppableDocumentsForDataOfType(Internal, sender as FrameworkElement, where);

                    foreach (var doc in targets)
                    {
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
        public void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (XPreviewRect.IsInVisualTree() && XPreviewRect.GetBoundingRect(this).Contains(e.GetCurrentPoint(this).Position))
            {
                e.Handled = true;
            }
        }

        private CoreCursor IBeam = new CoreCursor(CoreCursorType.IBeam, 1);
        private CoreCursor Cross = new CoreCursor(CoreCursorType.Cross, 1);
        public void LayoutRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!this.IsCtrlPressed() && !this.IsLeftBtnPressed() && !this.IsRightBtnPressed())
            {
                Window.Current.CoreWindow.PointerCursor = CurrentAnnotationType == AnnotationType.Region ? Cross : IBeam;

                e.Handled = true;
            }
        }

    }
}
