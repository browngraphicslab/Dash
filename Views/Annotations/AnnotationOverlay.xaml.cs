using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Dash.Annotations;
using NewControls.Geometry;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using static Dash.DataTransferTypeInfo;
using Selection = Dash.AnchorableAnnotation.Selection;

namespace Dash
{
    public partial class AnnotationOverlay : UserControl, ILinkHandler, INotifyPropertyChanged
    {
        private InkController _inkController;
        private AnnotationType _currAnnotationType = AnnotationType.None;
        private bool           _maskInkUpdates = false;
        private bool           _regionsInitialized =false;
        [CanBeNull] private AnchorableAnnotation    _currentAnnotation;
        private readonly Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();

        public event PropertyChangedEventHandler  PropertyChanged;
        public delegate DocumentController RegionGetter(AnnotationType type);
        public readonly DocumentController        MainDocument;
        public Selection                          SelectedRegion;
        public List<SelectableElement>            TextSelectableElements;
        public List<AnchorableAnnotation>         CurrentAnchorableAnnotations = new List<AnchorableAnnotation>();
        public ListController<DocumentController> RegionDocsList; // shortcut to the region documents stored in the RegionsKey
        public ListController<DocumentController> EmbeddedDocsList => AnnotationOverlayEmbeddings.EmbeddedDocsList; // shortcut to the embedded documents stored in the EmbeddedDocs Key
        public IEnumerable<Selection> SelectableRegions => XAnnotationCanvas.Children.OfType<AnchorableAnnotation>().Select((a) => a.ViewModel).Where((a) => a != null);
        public AnnotationType CurrentAnnotationType
        {
            get => _currAnnotationType;
            set
            {
                _currAnnotationType = value;
                OnPropertyChanged();

                if (this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas != null)
                {
                    this.GetFirstAncestorOfType<PdfAnnotationView>().XInkCanvas.InkPresenter.IsInputEnabled = _currAnnotationType == AnnotationType.Ink;
                    this.GetFirstAncestorOfType<PdfAnnotationView>().XInkCanvas.IsHitTestVisible = _currAnnotationType == AnnotationType.Ink;
                }
            }
        }
        public List<int> PageEndIndices { get; set; }
        private InkCanvas XInkCanvas { get; }
        public AnnotationOverlayEmbeddings AnnotationOverlayEmbeddings { get; set; }

        public AnnotationOverlay([NotNull] DocumentController viewDocument, bool delayLoadingRegions = false)
        {
            InitializeComponent();

            MainDocument = viewDocument;

            AnnotationOverlayEmbeddings = new AnnotationOverlayEmbeddings(this);

           
            //if (MainPage.Instance.xSettingsView.UseInkCanvas)
            //{
            if (this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas != null)
            {
                this.GetFirstAncestorOfType<PdfAnnotationView>().XInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen; //| CoreInputDeviceTypes.Touch;
                this.GetFirstAncestorOfType<PdfAnnotationView>().XInkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
                this.GetFirstAncestorOfType<PdfAnnotationView>().XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
                this.GetFirstAncestorOfType<PdfAnnotationView>().XInkCanvas.InkPresenter.IsInputEnabled = false;
                this.GetFirstAncestorOfType<PdfAnnotationView>().XInkCanvas.IsHitTestVisible = false;
            }
            
               // this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
          //  }

            RegionDocsList = MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);
            _inkController = MainDocument.GetDataDocument().GetFieldOrCreateDefault<InkController>(KeyStore.InkDataKey);
            MainDocument.GetDataDocument().AddWeakFieldUpdatedListener(this, KeyStore.InkDataKey, (view, controller, arge) => view.inkController_FieldModelUpdated(controller, arge));
            MainDocument.GetDataDocument().AddWeakFieldUpdatedListener(this, KeyStore.RegionsKey, (view, controller, arge) => view.regionDocsListOnFieldModelUpdated(controller, arge));


            Loaded += onLoaded;

            MainDocument.AddWeakFieldUpdatedListener(this, KeyStore.GoToRegionKey, (overlay, controller, arg3) => overlay.GoToUpdatedFieldChanged(controller, arg3));

            if (!delayLoadingRegions)
            {
                InitializeRegions();
            }
        }
        private void GoToUpdatedFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var newValue = args?.NewValue != null ? args.NewValue as DocumentController : sender.GetField<DocumentController>(KeyStore.GoToRegionKey);
            if (newValue != null && (sender.GetField(KeyStore.GoToRegionKey) != null || sender.GetField(KeyStore.GoToRegionLinkKey) != null))
            {
                SelectRegion(newValue);
            }
        }
        public void InitializeRegions()
        {
            _regionsInitialized = true;
            AddRegions(RegionDocsList, false);
            if (MainDocument.GetField(KeyStore.GoToRegionKey) != null)
            {
                GoToUpdatedFieldChanged(MainDocument, null);
            }
        }
        public void RemoveRegions(IEnumerable<DocumentController> oldItems)
        {
            XAnnotationCanvas.Children.OfType<RegionAnnotation>().Where((reg) => oldItems.Contains(reg.RegionDocumentController)).
                ToList().ForEach((reg) => XAnnotationCanvas.Children.Remove(reg));
        }
        public void AddRegions(IEnumerable<DocumentController> newItems, bool updateDocument = true)
        {
            if (_regionsInitialized)
            {
                foreach (var anno in newItems)
                {
                    if (updateDocument)
                    {
                        var listField = anno.GetDataDocument().GetDereferencedField<ListController<RectController>>(KeyStore.SelectionBoundsKey, null)?.ToList() ?? new List<RectController>();
                        if (!listField.Any())
                        {
                            var range = anno.GetField<ListController<PointController>>(KeyStore.SelectionIndicesListKey)?.FirstOrDefault()?.Data ?? new Point(0, -1);
                            for (double i = range.X; i <= range.Y; i++)
                            {
                                listField.Add(new RectController(TextSelectableElements[(int)i].Bounds));
                            }
                            anno.GetDataDocument().SetField<ListController<RectController>>(KeyStore.SelectionBoundsKey, listField, true);
                        }
                    }
                    XAnnotationCanvas.Children.Add(anno.CreateAnnotationAnchor(this));
                }
            }
        }

        public void SelectRegion(DocumentController region)
        {
            DeselectRegions();
            if (this.GetDocumentView() is DocumentView docView)
            {
                docView.Visibility = Visibility.Visible;
                foreach (var nvo in docView.GetDescendantsOfType<AnnotationOverlay>())
                {
                    foreach (var r in nvo.SelectableRegions.Where(r => r.RegionDocument.Equals(region)))
                    {
                        nvo.SelectedRegion = r;
                        r.IsSelected = true;
                    }
                }
            }
        }
        public void DeselectRegions()
        {
            if (this.GetDocumentView() is DocumentView docView)
            {
                foreach (var nvo in docView.GetDescendantsOfType<AnnotationOverlay>().Where((a) => a.SelectedRegion != null))
                {
                    nvo.SelectedRegion.IsSelected = false;
                    nvo.SelectedRegion = null;
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
                    var containingDocumentView = this.GetDocumentView();
                    await rtb.RenderAsync(containingDocumentView, (int)containingDocumentView.ActualWidth, (int)containingDocumentView.ActualHeight);

                    byte[] buf = (await rtb.GetPixelsAsync()).ToArray();
                    var bitmap = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
                    bitmap.PixelBuffer.AsStream().Write(buf, 0, buf.Length);

                    annotation = await new ImageToDashUtil().ParseBitmapAsync(bitmap);

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
            var annotation = new RichTextNote().Document;
            annotation.SetPosition(point);
            annotation.SetWidth(90);
            annotation.SetHeight(90);
            annotation.GetDataDocument().SetAnnotationType(AnnotationType.Pin);
            annotation.GetDataDocument().SetRegionDefinition(MainDocument);
            annotation.AddToListField(KeyStore.SelectionRegionTopLeftKey, new PointController(point.X, point.Y));
            annotation.AddToListField(KeyStore.SelectionRegionSizeKey, new PointController(1, 1));
            if (linkedDoc != null)
            {
                annotation.Link(linkedDoc, LinkBehavior.Overlay, null);
            }

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

        void onLoaded(object o, RoutedEventArgs routedEventArgs)
        {
            _inkController = MainDocument.GetDataDocument().GetFieldOrCreateDefault<InkController>(KeyStore.InkDataKey);
            this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
           // _inkController.FieldModelUpdated += inkController_FieldModelUpdated;
        }


        private void regionDocsListOnFieldModelUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs args)
        {
            if (args is DocumentController.DocumentFieldUpdatedEventArgs dargs && dargs.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs listArgs)
            {
                switch (listArgs.ListAction)
                {
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add: AddRegions(listArgs.NewItems); break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove: RemoveRegions(listArgs.OldItems); break;
                }
            }
        }

        private void inkController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args)
        {
            if (!_maskInkUpdates && this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas != null)
            {
                this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas.InkPresenter.StrokeContainer.Clear();
                this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(_inkController.GetStrokes().Select(s => s.Clone()));
            }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static void LinkRegion(DocumentController sourceDoc, DocumentController targetDoc,
            double? sStartIndex = null, double? sEndIndex = null, double? tStartIndex = null, double? tEndIndex = null,
            string linkTag = null, string behavior = null)
        {
            Debug.Assert(sourceDoc.GetRegionDefinition() == null);
            var linkSource = sStartIndex is double sStart && sEndIndex is double sEnd
                ? createRegionDoc(sourceDoc, sStart, sEnd)
                : sourceDoc;
            var linkTarget = tStartIndex is double tStart && tEndIndex is double tEnd
                ? createRegionDoc(targetDoc, tStart, tEnd)
                : targetDoc;

            linkSource.Link(linkTarget, Enum.TryParse(behavior, out LinkBehavior behaviorEnum) ? behaviorEnum : LinkBehavior.Annotate, linkTag);

            DocumentController createRegionDoc(DocumentController regionContainerDocument, double start, double end)
            {
                var region = ExistingRegionAtIndices(regionContainerDocument, start, end);
                if (region == null)
                {
                    region = new RichTextNote().Document;
                    region.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionIndicesListKey).Add(new PointController(start, end));
                    region.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
                    region.GetFieldOrCreateDefault<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);
                    region.SetAnnotationType(AnnotationType.Selection);
                    region.SetRegionDefinition(regionContainerDocument);
                    region.SetTitle(regionContainerDocument.Title + " region");

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
                    return (selInds != null && selInds.Count == 1 && ((int)startIndex == (int)selInds[0].Data.X &&
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
            if (XPreviewRect.GetBoundingRect(this).Contains(point)) // create annotation for preview region containing 'point'
            {
                embeddedDocument = embeddedDocument ?? createEmbeddedTextNote(this, point);
                EmbeddedDocsList.Add(embeddedDocument);
                StartAnnotation(AnnotationType.Region, point, new Selection(await CreateRegionFromPreviewOrSelection(embeddedDocument)));
                EndAnnotation(point);
            }
            else
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
                if (_currentAnnotation == null)  // if no other pin exists with 4x4 of 'point', then create a pushin
                {
                    embeddedDocument = embeddedDocument ?? createEmbeddedTextNote(this, point);
                    EmbeddedDocsList.Add(embeddedDocument);
                    StartAnnotation(AnnotationType.Pin, point, new Selection(CreatePinRegion(point, embeddedDocument)));
                    EndAnnotation(point);
                }
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
                DeselectRegions();
            }
        }

        private static DocumentController createEmbeddedTextNote(AnnotationOverlay parent, Point where)
        {
            var richText = new RichTextNote("<annotation>", new Point(where.X + 10, where.Y + 10));
            richText.Document.SetField(KeyStore.BackgroundColorKey, new TextController(Colors.White.ToString()), true);
            return richText.Document;
        }
        #endregion

        #region Ink Annotation

        private void InkPresenterOnStrokesErased(InkPresenter inkPresenter, InkStrokesErasedEventArgs inkStrokesErasedEventArgs)
        {
            if (this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas != null)
            {
                _maskInkUpdates = true;
                _inkController.UpdateStrokesFromList(this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                _maskInkUpdates = false;
            }
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            if (this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas != null)
            {
                _maskInkUpdates = true;
                _inkController.UpdateStrokesFromList(this.GetFirstAncestorOfType<PdfAnnotationView>()?.XInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                _maskInkUpdates = false;
            }
        }

        #endregion

        #region Selection Annotation

        public void ClearSelection(bool hardReset = false)
        {
            CurrentAnchorableAnnotations.Clear();
            _currRect = null;
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
        public void DeselectIndex(int index, Rect? clipRect = null, int endIndex = -1)
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
                bool clipRectNonexistent = clipRect == null || clipRect == Rect.Empty;
                bool clipRectContainsIndex = clipRect?.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2,
                                                ele.Bounds.Y + ele.Bounds.Height / 2)) == true;
                if (clipRectNonexistent || clipRectContainsIndex)
                {
                    var currRect = _selectedRectangles[index];
                    double left = Canvas.GetLeft(currRect);
                    double right = Canvas.GetLeft(currRect) + currRect.Width;
                    double top = Canvas.GetTop(currRect);
                    if (endIndex != -1)
                    {
                        // if we're deselecting text backwards
                        if (ele.Bounds.Left - left < ele.Bounds.Width)
                        {
                            bool farEnoughY = Math.Abs(top -
                                                      TextSelectableElements[index + 1].Bounds.Top) >
                                             TextSelectableElements[index].Bounds.Height / 5;
                            bool farEnoughX = Math.Abs(left - TextSelectableElements[index + 1].Bounds.Left) >
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
                            bool farEnoughY = Math.Abs(top -
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

        public void SelectIndex(int index, Rect? clipRect = null, Brush highlightBrush = null)
        {
            highlightBrush = highlightBrush ?? new SolidColorBrush(Color.FromArgb(120, 0x94, 0xA5, 0xBB));
            var ele = TextSelectableElements[index];
            bool clipRectNonexistent = clipRect == null || clipRect == Rect.Empty;
            bool clipRectContainsIndex = clipRect?.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2,
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
                    Fill = highlightBrush
                };
                Canvas.SetLeft(_currRect, ele.Bounds.Left);
                Canvas.SetTop(_currRect, ele.Bounds.Top);
                XSelectionCanvas.Children.Add(_currRect);
            }

            double left = Canvas.GetLeft(_currRect);
            double right = Canvas.GetLeft(_currRect) + _currRect.Width;
            double top = Canvas.GetTop(_currRect);
            bool closeEnoughX = Math.Abs(ele.Bounds.Left - right) <
                               ele.Bounds.Width * 4;
            bool closeEnoughY = Math.Abs(ele.Bounds.Top - top) <
                              ele.Bounds.Height / 5;
            bool similarSize = ele.Bounds.Height - _currRect.Height < ele.Bounds.Height;
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
                    Fill = highlightBrush
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

                int currentSelectionStart = textAnnotation.StartIndex;
                int currentSelectionEnd = textAnnotation.EndIndex;
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
                        for (int i = startIndex; i <= endIndex; ++i)
                        {
                            SelectIndex(i, currentClipRect);
                        }
                    }
                    else
                    {
                        for (int i = startIndex; i < currentSelectionStart; ++i)
                        {
                            SelectIndex(i);
                        }

                        for (int i = currentSelectionStart; i < startIndex; ++i)
                        {
                            DeselectIndex(i, null, startIndex - 1);
                        }

                        for (int i = currentSelectionEnd + 1; i <= endIndex; ++i)
                        {
                            SelectIndex(i);
                        }

                        for (int i = endIndex + 1; i <= currentSelectionEnd; ++i)
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
                double rTop = (rect.RenderTransform as TranslateTransform).Y;
                bool belowTopBound = rTop + rect.Height > currentClipRect.Top;
                bool belowBottomBound = rTop < currentClipRect.Bottom;
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
                foreach (int key in _selectedRectangles.Where(kvp => kvp.Value.Equals(r)).Select(kvp => kvp.Key))
                {
                    keys.Add(key);
                }

                foreach (int key in keys)
                {
                    _selectedRectangles.Remove(key);
                }
            });

            int startPage = GetPageOf(currentClipRect.Top);
            int endPage = GetPageOf(currentClipRect.Bottom);
            // startIndex is either 0 or the last page's end index + 1
            int startIndex = startPage > 0 ? PageEndIndices[startPage - 1] + 1 : 0;
            int endIndex = PageEndIndices[endPage];

            // loop through the indices between the possible pages
            for (int index = startIndex; index <= endIndex; index++)
            {
                var ele = TextSelectableElements[index];
                if (currentClipRect.Contains(new Point(ele.Bounds.X + ele.Bounds.Width / 2,
                        ele.Bounds.Y + ele.Bounds.Height / 2)))
                {
                    bool found = false;
                    foreach (var rect in _clipRectSelections)
                    {
                        double rLeft = (rect.RenderTransform as TranslateTransform).X;
                        double rTop = (rect.RenderTransform as TranslateTransform).Y;
                        bool closeEnoughX = Math.Abs(ele.Bounds.Left - rLeft) < ele.Bounds.Width + rect.Width;
                        bool closeEnoughY = Math.Abs(ele.Bounds.Top - rTop) < ele.Bounds.Height / 5;
                        bool similarSize = ele.Bounds.Height - rect.Height < ele.Bounds.Height;

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
            double currOffset = 0.0;
            int i = 0;
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
                var val  = AnnotationOverlayEmbeddings.GetDescendantsOfType<DocumentView>().FirstOrDefault(dv => dv.ViewModel.DataDocument.Equals(dest.GetDataDocument()));
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
            if (e.DataView.HasDataOfType(Internal) && !this.IsAltPressed() && !this.IsCtrlPressed())
            {
                e.Handled = true;
                var dm = e.DataView.GetDragModel() as DragDocumentModel;
                if (!this.IsShiftPressed() && dm?.DraggingLinkButton == false)
                {
                    // if docs are being moved within the overlay, then they will be placed appropriately and returned from this call.
                    // if docs are being dragged onto this overlay, we disallow that and no droppedDocs are returned from this call.
                    var droppedDocs = await e.DataView.GetDroppableDocumentsForDataOfType(Any, sender as FrameworkElement, where);
                    if (droppedDocs.Count > 0)
                    {
                        for (int i = 0; i < dm.DraggedDocCollectionViews?.Count; i++)
                        {
                            if (dm.DraggedDocumentViews != null)
                            {
                                if (!AnnotationOverlayEmbeddings.GetDescendants().Contains(dm.DraggedDocumentViews[i]))
                                {
                                    EmbeddedDocsList.Add(droppedDocs[i]);
                                }
                                var overlayEmbeddings = dm.DraggedDocumentViews[i]?.GetFirstAncestorOfType<AnnotationOverlayEmbeddings>();
                                if (AnnotationOverlayEmbeddings != overlayEmbeddings)
                                {
                                    overlayEmbeddings?.EmbeddedDocsList.Remove(dm.DraggedDocuments[i]);
                                }
                                MainPage.Instance.ClearFloatingDoc(dm.DraggedDocumentViews[i]);
                            }

                            dm.DraggedDocCollectionViews[i]?.RemoveDocument(dm.DraggedDocuments[i]);
                        }
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                    else
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                    }
                }
                else if (dm?.DraggingLinkButton == true && !this.IsShiftPressed())
                {
                    var docView = this.GetDocumentView();
                    docView.MakeDocumentLink(e.GetPosition(docView), dm);
                }
                else
                {
                    foreach (var doc in await e.DataView.GetDroppableDocumentsForDataOfType(Internal, sender as FrameworkElement, where))
                    {
                        if (doc.GetActualSize().X > 200 &&
                            !doc.DocumentType.Equals(RichTextBox.DocumentType) && !doc.DocumentType.Equals(TextingBox.DocumentType))
                        {
                            doc.SetWidth(200);
                        }

                        EmbedDocumentWithPin(where, doc);
                    }
                }
            }
            // if we drag from the file system
            if (e.DataView?.Contains(StandardDataFormats.StorageItems) == true)
            {
                e.Handled = true;
                try
                {
                    if ((await FileDropHelper.HandleDrop(e.DataView, where)) is DocumentController target)
                    {
                        EmbedDocumentWithPin(where, target);
                        if (!target.DocumentType.Equals(RichTextBox.DocumentType) && !target.DocumentType.Equals(TextingBox.DocumentType))
                        {
                            target.SetWidth(200);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }
            }
        }

        private void XPreviewRect_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true; // prevents a new annotation from being started -- allows double clicking to create an annotation
        }

        private CoreCursor IBeam = new CoreCursor(CoreCursorType.IBeam, 1);
        private CoreCursor Cross = new CoreCursor(CoreCursorType.Cross, 1);
        private void LayoutRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!this.IsCtrlPressed() && !(this.IsLeftBtnPressed() || e.Pointer.PointerDeviceType == PointerDeviceType.Pen) && !this.IsRightBtnPressed() && this.GetDocumentView().ViewModel.IsSelected)
            {
                Window.Current.CoreWindow.PointerCursor = CurrentAnnotationType == AnnotationType.Region ? Cross : IBeam;

                e.Handled = true;
            }
        }

        public void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (XPreviewRect.IsInVisualTree() && XPreviewRect.GetBoundingRect(this).Contains(e.GetCurrentPoint(this).Position))
            {
                e.Handled = true;
            }
        }

        public void AnnotationOverlayDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (this.GetDocumentView().ViewModel.IsSelected && CurrentAnnotationType == AnnotationType.Region)
            {
                using (UndoManager.GetBatchHandle())
                {
                    EmbedDocumentWithPin(e.GetPosition(this));
                }
                DeselectRegions();
                _currentAnnotation = null;
                e.Handled = true;
            }
        }

        private void LayoutRoot_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            
        }
    }
}
