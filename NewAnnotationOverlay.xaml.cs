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
        private readonly RegionGetter _regionGetter;
        private readonly ListController<DocumentController> _regionList;

        public delegate DocumentController RegionGetter(AnnotationType type);

        private readonly AnnotationManager _annotationManager;

        private ISelectable _selectedRegion;

        public event EventHandler<DocumentController> RegionAdded;
        public event EventHandler<DocumentController> RegionRemoved;

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
                _mainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RegionsKey);

            foreach (var documentController in _regionList)
            {
                switch (documentController.GetAnnotationType())
                {
                    case AnnotationType.Region:
                        RenderRegion(documentController);
                        break;
                    case AnnotationType.Selection:
                        RenderTextAnnotation(documentController);
                        break;
                    case AnnotationType.Ink:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void SetAnnotationType(AnnotationType type)
        {
            if (type != _currentAnnotationType)
            {
                ClearPreviewRegion();
                ClearTextSelection();
            }
            _currentAnnotationType = type;
        }

        public DocumentController GetRegionDoc()
        {
            if (_selectedRegion != null)
            {
                return _selectedRegion.RegionDocument;
            }

            DocumentController annotation;
            switch (_currentAnnotationType)
            {
                case AnnotationType.Region:
                    if (XPreviewRect.Visibility == Visibility.Collapsed)
                    {
                        goto case AnnotationType.None;
                    }
                    annotation = _regionGetter(_currentAnnotationType);
                    var pos = new Point(Canvas.GetLeft(XPreviewRect), Canvas.GetTop(XPreviewRect));
                    var size = new Size(XPreviewRect.Width, XPreviewRect.Height);
                    annotation.SetPosition(pos);
                    annotation.SetWidth(XPreviewRect.Width);
                    annotation.SetHeight(XPreviewRect.Height);
                    RenderRegion(annotation);
                    ClearPreviewRegion();
                    break;
                case AnnotationType.Selection:
                    if (_currentSelectionStart == -1 || _currentSelectionEnd == -1)
                    {
                        goto case AnnotationType.None;
                    }

                    annotation = _regionGetter(_currentAnnotationType);
                    var posList = new ListController<PointController>();
                    var sizeList = new ListController<PointController>();
                    double minY = double.PositiveInfinity;
                    for (int i = _currentSelectionStart; i <= _currentSelectionEnd; ++i)
                    {
                        var se = _textSelectableElements[i];
                        posList.Add(new PointController(se.Bounds.X, se.Bounds.Y));
                        sizeList.Add(new PointController(se.Bounds.Width, se.Bounds.Height));
                        minY = Math.Min(minY, se.Bounds.Y);
                    }

                    //TODO Add ListController.DeferUpdate
                    annotation.SetField(KeyStore.SelectionRegionTopLeftKey, posList, true);
                    annotation.SetField(KeyStore.SelectionRegionSizeKey, sizeList, true);
                    annotation.SetPosition(new Point(0, minY));
                    RenderTextAnnotation(annotation);
                    ClearTextSelection();
                    break;
                case AnnotationType.Ink:
                case AnnotationType.None:
                    return _mainDocument;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Debug.Assert(annotation != null, "Annotation must be assigned in the switch statement");
            Debug.Assert(!(annotation.Equals(_mainDocument)), "If returning the main document, return it immediately, don't fall through to here");
            _regionList.Add(annotation);
            annotation.SetRegionDefinition(_mainDocument);
            annotation.SetAnnotationType(_currentAnnotationType);
            RegionAdded?.Invoke(this, annotation);
            return annotation;
        }

        #region General Annotation

        public void StartAnnotation(Point p)
        {
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

        #region Region Annotation

        public void ClearPreviewRegion()
        {
            XPreviewRect.Visibility = Visibility.Collapsed;
        }

        private Point _previewStartPoint;
        public void StartRegion(Point p)
        {
            if (_currentAnnotationType != AnnotationType.Region)
            {
                return;
            }
            _previewStartPoint = p;
            Canvas.SetLeft(XPreviewRect, p.X);
            Canvas.SetTop(XPreviewRect, p.Y);
            XPreviewRect.Width = 0;
            XPreviewRect.Height = 0;
            XPreviewRect.Visibility = Visibility.Visible;
        }

        public void UpdateRegion(Point p)
        {
            if (_currentAnnotationType != AnnotationType.Region)
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
        }

        private void RenderRegion(DocumentController region)
        {
            var r = new RegionAnnotation(region);
            r.Tapped += (sender, args) =>
            {
                SelectRegion(sender as ISelectable, args.GetPosition(this));
                args.Handled = true;
            };
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

        private List<SelectableElement> _textSelectableElements;

        public void SetSelectableElements(IEnumerable<SelectableElement> selectableElements)
        {
            _textSelectableElements = selectableElements.ToList();
        }

        public void ClearTextSelection()
        {
            _currentSelectionStart = -1;
            _currentSelectionEnd = -1;
            _selectionStartPoint = null;
            _selectedRectangles.Clear();
            XSelectionCanvas.Children.Clear();
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
            if (_currentSelectionStart == -1) return;//Not currently selecting anything
            _selectionStartPoint = null;
        }

        private void RenderTextAnnotation(DocumentController region)
        {
            var posList = region.GetField<ListController<PointController>>(KeyStore.SelectionRegionTopLeftKey);
            var sizeList = region.GetField<ListController<PointController>>(KeyStore.SelectionRegionSizeKey);
            Debug.Assert(posList.Count == sizeList.Count);

            SelectionViewModel vm = new SelectionViewModel(region);
            for (int i = 0; i < posList.Count; ++i)
            {
                RenderTextRegion(posList[i].Data, sizeList[i].Data, vm);
            }
        }

        private void RenderTextRegion(Point pos, Point size, SelectionViewModel vm)
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
        private int _currentSelectionStart = -1, _currentSelectionEnd = -1;
        private void SelectElements(int startIndex, int endIndex)
        {
            if (_currentSelectionStart == -1)
            {
                Debug.Assert(_currentSelectionEnd == -1);
                for (var i = startIndex; i <= endIndex; ++i)
                {
                    SelectIndex(i);
                }
            }
            else
            {
                for (var i = startIndex; i < _currentSelectionStart; ++i)
                {
                    SelectIndex(i);
                }
                for (var i = _currentSelectionStart; i < startIndex; ++i)
                {
                    DeselectIndex(i);
                }
                for (var i = _currentSelectionEnd + 1; i <= endIndex; ++i)
                {
                    SelectIndex(i);
                }
                for (var i = endIndex + 1; i <= _currentSelectionEnd; ++i)
                {
                    DeselectIndex(i);
                }
            }

            _currentSelectionStart = startIndex;
            _currentSelectionEnd = endIndex;

        }

        #endregion

        #endregion

    }
}
