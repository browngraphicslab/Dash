﻿using System;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{

    public enum AnnotationType
    {
        None,
        Region,
        Selection,
        Ink
    }

    public interface ISelectable
    {
        void Select();
        void Deselect();

        bool Selected { get; }

        DocumentController RegionDocument { get; }
    }

    public sealed partial class NewAnnotationOverlay : UserControl
    {
        private AnnotationType _currentAnnotationType = AnnotationType.None;

        private readonly DocumentController _mainDocument;
        public readonly RegionGetter _regionGetter;
        private readonly ListController<DocumentController> _regionList;
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
                return;
            }
            _selectedRegion?.Deselect();
            _selectedRegion = _regions.FirstOrDefault(sel => sel.RegionDocument.Equals(region));
            _selectedRegion?.Select();
        }

        private void SelectRegion(ISelectable selectable, Point? mousePos)
        {
            if (_selectedRegion == selectable)
            {
                return;
            }
            _selectedRegion?.Deselect();
            _selectedRegion = selectable;
            _annotationManager.FollowRegion(selectable.RegionDocument, this.GetAncestorsOfType<ILinkHandler>(), mousePos ?? new Point(0, 0));
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

            _regionList =
                _mainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);
            _inkController = _mainDocument.GetDataDocument()
                .GetFieldOrCreateDefault<InkController>(KeyStore.InkDataKey);

            foreach (var documentController in _regionList)
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
                    case AnnotationType.Region:
                    case AnnotationType.Selection:
                        RenderRegion(documentController);
                        break;
                    case AnnotationType.Ink:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }

        private void OnUnloaded(object o, RoutedEventArgs routedEventArgs)
        {
            _regionList.FieldModelUpdated -= RegionListOnFieldModelUpdated;
            _inkController.FieldModelUpdated -= _inkController_FieldModelUpdated;
        }

        private void OnLoaded(object o, RoutedEventArgs routedEventArgs)
        {
            _inkController.FieldModelUpdated += _inkController_FieldModelUpdated;
            _regionList.FieldModelUpdated += RegionListOnFieldModelUpdated;
        }

        private void RegionListOnFieldModelUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs fieldUpdatedEventArgs, Context context)
        {
            var listArgs = fieldUpdatedEventArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs;
            if (listArgs == null)
            {
                return;
            }
            switch (listArgs.ListAction)
            {
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                    foreach (var documentController in listArgs.NewItems)
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
                    var subRegionsOffsets = new List<double>();
                    var minRegionY = double.PositiveInfinity;
                    foreach (var rect in _regionRectangles)
                    {
                        regionPosList.Add(new PointController(rect.X, rect.Y));
                        regionSizeList.Add(new PointController(rect.Width, rect.Height));
                        var pdfView = this.GetFirstAncestorOfType<CustomPdfView>();
                        var scale = pdfView.Width / pdfView.PdfMaxWidth;
                        var vOffset = rect.Y * scale;
                        var scrollRatio = vOffset / pdfView.TopScrollViewer.ExtentHeight;
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
                            if (!indices.Contains(i))
                            {
                                indices.Add(i);
                            }
                        }
                    }

                    int prevIndex = -1; 
                    foreach (var index in indices)
                    {
                        var elem = _textSelectableElements[index];
                        if (prevIndex + 1 != index)
                        {
                            var pdfView = this.GetFirstAncestorOfType<CustomPdfView>();
                            var scale = pdfView.Width / pdfView.PdfMaxWidth;
                            var vOffset = elem.Bounds.Y * scale;
                            var scrollRatio = vOffset / pdfView.TopScrollViewer.ExtentHeight;
                            subRegionsOffsets.Add(scrollRatio);
                        }
                        regionPosList.Add(new PointController(elem.Bounds.X, elem.Bounds.Y));
                        regionSizeList.Add(new PointController(elem.Bounds.Width, elem.Bounds.Height));
                        minRegionY = Math.Min(minRegionY, elem.Bounds.Y);
                        prevIndex = index;
                    }

                    subRegionsOffsets.Sort((y1, y2) => Math.Sign(y1 - y2));

                    //TODO Add ListController.DeferUpdate
                    annotation.SetField(KeyStore.SelectionRegionTopLeftKey, regionPosList, true);
                    annotation.SetField(KeyStore.SelectionRegionSizeKey, regionSizeList, true);
                    annotation.SetField(KeyStore.PDFSubregionKey,
                        new ListController<NumberController>(
                            subRegionsOffsets.ConvertAll(i => new NumberController(i))), true);
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
            Debug.Assert(!(annotation.Equals(_mainDocument)),
                "If returning the main document, return it immediately, don't fall through to here");
            annotation.SetRegionDefinition(_mainDocument);
            annotation.SetAnnotationType(_currentAnnotationType);
            _regionList.Add(annotation);
            RegionAdded?.Invoke(this, annotation);

            return annotation;
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
        }

        public void EndRegion(Point p)
        {
            if (_currentAnnotationType != AnnotationType.Region)
            {
                return;
            }
            _annotatingRegion = false;
            var lastRect = _regionRectangles.Last();
            _regionRectangles[_regionRectangles.Count - 1] =
                new Rect(Canvas.GetLeft(XPreviewRect), Canvas.GetTop(XPreviewRect), XPreviewRect.Width,
                    XPreviewRect.Height);
            var viewRect = new Rectangle {Width = XPreviewRect.Width, Height = XPreviewRect.Height, Fill = XPreviewRect.Fill};
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
            Canvas.SetZIndex(r, 1000000);
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

            private readonly SolidColorBrush _selectedBrush = new SolidColorBrush(Color.FromArgb(60, 0, 255, 0));
            private readonly SolidColorBrush _unselectedBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 0));

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
            var removeItems = XAnnotationCanvas.Children.Where(i => !((i as Rectangle)?.DataContext is SelectionViewModel) && i != XPreviewRect).ToList();
            if (XAnnotationCanvas.Children.Any())
            {
                var lastAdded = XAnnotationCanvas.Children.Last();
                if (!((lastAdded as Rectangle)?.DataContext is SelectionViewModel))
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
            Debug.Assert(posList.Count == sizeList.Count);

            SelectionViewModel vm = new SelectionViewModel(region);
            for (int i = 0; i < posList.Count; ++i)
            {
                RenderSubRegion(posList[i].Data, sizeList[i].Data, vm);
            }

            _regions.Add(vm);
        }

        private void RenderSubRegion(Point pos, Point size, SelectionViewModel vm)
        {
            Rectangle r = new Rectangle
            {
                Width = size.X,
                Height = size.Y,
                Fill = new SolidColorBrush(Color.FromArgb(80, 0xFF, 0xFF, 0x00)),
                DataContext = vm
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
            if (!this.IsCtrlPressed())
            {
                if (_currentSelections.Count > 1 || _regionRectangles.Any())
                {
                    ClearSelection();
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

        #endregion

        #endregion

    }
}
