using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using System.Collections.ObjectModel;

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
            if (region.Equals(_selectedRegion?.RegionDocument))
            {
                if (this.GetFirstAncestorOfType<DocumentView>().Visibility.Equals(Visibility.Collapsed))
                {
                    this.GetFirstAncestorOfType<DocumentView>().Visibility = Visibility.Visible;
                }

                if (_selectedRegion.Selected)
                {
                    _selectedRegion.Deselect();
                    var vm = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel;
                    if (vm != null)
                    {
                        vm.SearchHighlightState = new Thickness(0);
                    }
                }
                else
                {
                    _selectedRegion.Select();
                    var vm = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel;
                    if (vm != null)
                    {
                        vm.SearchHighlightState = new Thickness(8);
                    };
                }
                return;
            }
            _selectedRegion?.Deselect();
            _selectedRegion = _regions.FirstOrDefault(sel => sel.RegionDocument.Equals(region));
            _selectedRegion?.Select();
        }

        private void SelectRegion(ISelectable selectable, Point? mousePos)
        {
            // get the list of linkhandlers starting from this all the way up to the mainpage
            var linkHandlers = this.GetAncestorsOfType<ILinkHandler>().ToList();
            // NewAnnotationOverlay is an ILinkHandler but isn't included in GetAncestorsOfType()
            linkHandlers.Insert(0, this);
            _annotationManager.FollowRegion(selectable.RegionDocument, linkHandlers, mousePos ?? new Point(0, 0));

            // we still want to follow the region even if it's already selected, so this code's position matters
            if (_selectedRegion == selectable)
            {
                return;
            }
            _selectedRegion?.Deselect();
            _selectedRegion = selectable;
            _selectedRegion.Select();

        }

        private void DeselectRegion()
        {
            _selectedRegion?.Deselect();
            _selectedRegion = null;
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

            foreach (var documentController in RegionDocsList)
            {
                RenderAnnotation(documentController);
            }

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
                    if (!_regionRectangles.Any() && (!_currentSelections.Any() || _currentSelections.Last().Key == -1))
                    {
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
                        double scale = pdfView?.Width / pdfView?.PdfMaxWidth ?? 1;
                        double vOffset = rect.Y * scale;
                        double scrollRatio = vOffset / pdfView?.TopScrollViewer.ExtentHeight ?? 0;
                        subRegionsOffsets.Add(scrollRatio);
                        minRegionY = Math.Min(rect.Y, minRegionY);
                    }

                    // loop through each selection and add the indices in each selection set
                    var indices = new List<int>();
                    foreach (var selection in _currentSelections)
                    {
                        for (var i = selection.Key; i <= selection.Value; i++)
                        {
                            // this will avoid double selecting any items
                            if (!indices.Contains(i)) indices.Add(i);
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
                            double scale = pdfView.Width / pdfView.PdfMaxWidth;
                            double vOffset = elem.Bounds.Y * scale;
                            double scrollRatio = vOffset / pdfView.TopScrollViewer.ExtentHeight;
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

        public static DocumentController LinkRegion(int startIndex, int endIndex, DocumentController sourceDoc, DocumentController targetDoc)
        {
            var selectionIndexList = new ListController<PointController> { new PointController(startIndex, endIndex) };
            targetDoc.SetField(KeyStore.SelectionIndicesListKey, selectionIndexList, true);
            targetDoc.SetRegionDefinition(sourceDoc);
            targetDoc.SetAnnotationType(AnnotationType.Selection);
            sourceDoc.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey).Add(targetDoc);
            return targetDoc;
        }

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
                XAnnotationCanvas.Children.Add(XPreviewRect);
            }
            _regionRectangles.Add(new Rect(p.X, p.Y, 0, 0));
        }

        private void CreatePin(Point point)
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

            var richText = new RichTextNote("<annotation>", new Point(point.X + 10, point.Y + 10),
                new Size(150, 75));
            richText.Document.SetBackgroundColor(Colors.White);
            richText.Document.SetField(KeyStore.LinkContextKey, new TextController(nameof(LinkContexts.PushPin)), true);
            richText.Document.SetHidden(true); // hidden flag will be toggled off when annotation is rendered after annotation is added to RegionDocsList-- why??
            var annotation = _regionGetter(AnnotationType.Pin);
            annotation.SetPosition(new Point(point.X + 10, point.Y + 10));
            annotation.SetWidth(10);
            annotation.SetHeight(10);
            annotation.GetDataDocument().SetField(KeyStore.RegionTypeKey, new TextController(nameof(AnnotationType.Pin)), true);
            annotation.Link(richText.Document, LinkContexts.PushPin);
            RegionDocsList.Add(annotation);
            RegionAdded?.Invoke(this, annotation);
            RenderPin(annotation, richText.Document);
            var pdfView = this.GetFirstAncestorOfType<CustomPdfView>();

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

            var vm = new SelectionViewModel(region)
            {
                SelectedBrush = new SolidColorBrush(Colors.OrangeRed),
                UnselectedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0))
            };
            pin.DataContext = vm;
            
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

            SelectRegion(vm, new Point(point.X + pin.Width, point.Y + pin.Height));
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

            }

            var viewRect = new Rectangle
            {
                Width = XPreviewRect.Width,
                Height = XPreviewRect.Height,
                Fill = XPreviewRect.Fill
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

        private sealed class SelectionViewModel : INotifyPropertyChanged, ISelectable
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

            private SolidColorBrush _selectedBrush = new SolidColorBrush(Color.FromArgb(60, 0, 255, 0));

            public SolidColorBrush SelectedBrush
            {
                get => _selectedBrush;
                set => _selectedBrush = value;
            }
            private SolidColorBrush _unselectedBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 0));

            public SolidColorBrush UnselectedBrush
            {
                get => _unselectedBrush;
                set => _unselectedBrush = value;
            }

            public SelectionViewModel(DocumentController region)
            {
                RegionDocument = region;
                _selectionColor = _unselectedBrush;
            }

            public bool Selected { get; private set; } = false;

            public DocumentController RegionDocument { get; }

            public void Select()
            {
                SelectionColor = _selectedBrush;
                Selected = true;
            }

            public void Deselect()
            {
                SelectionColor = _unselectedBrush;
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
        }

        public void ClearSelection(bool hardReset = false)
        {
            _currentSelections.Clear();
            _selectionStartPoint = hardReset ? null : _selectionStartPoint;
            _selectedRectangles.Clear();
            XSelectionCanvas.Children.Clear();
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
            _currentSelections.Add(new KeyValuePair<int, int>(-1, -1));
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
                SelectElements(Math.Min(startEle.Index, currentEle.Index), Math.Max(startEle.Index, currentEle.Index));
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

            var vm = new SelectionViewModel(region);

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

            r.SetBinding(VisibilityProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(AnnotationVisibility)),
                Converter = new BoolToVisibilityConverter()
            });

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

        private void DeselectIndex(int index)
        {
            if (!_selectedRectangles.ContainsKey(index))
            {
                return;
            }

            XSelectionCanvas.Children.Remove(_selectedRectangles[index]);
            _selectedRectangles.Remove(index);
        }

        private readonly SolidColorBrush _selectionBrush = new SolidColorBrush(Color.FromArgb(120, 0x94, 0xA5, 0xBB));

        private void SelectIndex(int index)
        {
            if (_selectedRectangles.ContainsKey(index))
            {
                return;
            }

            var ele = _textSelectableElements[index];
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


        private Point? _selectionStartPoint;
        private Dictionary<int, Rectangle> _selectedRectangles = new Dictionary<int, Rectangle>();

        private void SelectElements(int startIndex, int endIndex)
        {// if control isn't pressed, reset the selection
     
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
    }

}
