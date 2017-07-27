using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Security.Cryptography.Core;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Shapes;

namespace Dash
{
    public partial class SelectableContainer : UserControl
    {
        private struct LinesAndTextBlocks
        {
            public LinesAndTextBlocks(Line hLine, Line vLine, Border widthBorder, Border heightBorder)
            {
                HLine = hLine;
                VLine = vLine;
                WidthBorder = widthBorder;
                HeightBorder = heightBorder;
            }
            public Line HLine { get; }
            public Line VLine { get; }
            public Border WidthBorder { get; }
            public Border HeightBorder { get; }
        }

        public delegate void OnSelectionChangedHandler(SelectableContainer sender, DocumentController layoutDocument, DocumentController dataDocument);
        public event OnSelectionChangedHandler OnSelectionChanged;


        private SelectableContainer _selectedLayoutContainer;
        private SelectableContainer _parentContainer;
        private List<SelectableContainer> _childContainers;
        private bool _isSelected;
        private FrameworkElement _contentElement;
        private List<Ellipse> _draggerList;
        private Dictionary<Ellipse, LinesAndTextBlocks> _lineMap;
        private Ellipse _pressedEllipse;

        public readonly DocumentController LayoutDocument;
        public readonly DocumentController DataDocument;
        private ManipulationControls _manipulator;
        private bool _isLowestSelected;
        private RootSnapManager _rootSnapManager;

        public FrameworkElement ContentElement
        {
            get { return _contentElement; }
            set
            {
                _contentElement = value;
                OnContentChanged();
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = IsRoot() || value;
                ContentElement.IsHitTestVisible = value;
                if (_isSelected)
                {
                    //XGrid.BorderThickness = new Thickness(3);
                }
                else
                {
                    //XGrid.BorderThickness = new Thickness(1);
                    IsLowestSelected = false;
                }
            }
        }

        public bool IsLowestSelected
        {
            get { return _isLowestSelected; }
            set
            {
                _isLowestSelected = value;
                SetEllipseVisibility();
            }
        }

        private void SetEllipseVisibility()
        {
            var isVisible = !IsRoot() && IsLowestSelected;

            foreach (var ellipse in _draggerList)
            {
                ellipse.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public SelectableContainer(FrameworkElement contentElement, DocumentController layoutDocument, DocumentController dataDocument = null)
        {
            InitializeComponent();
            InitiateManipulators();
            ContentElement = contentElement;
            ContentElement.SizeChanged += ContentElement_SizeChanged;
            LayoutDocument = layoutDocument;
            DataDocument = dataDocument;

            RenderTransform = new TranslateTransform();
            _childContainers = new List<SelectableContainer>();

            Loaded += SelectableContainer_Loaded;
            Unloaded += SelectableContainer_Unloaded;
            Tapped += CompositeLayoutContainer_Tapped;
        }

        private void SelectableContainer_Unloaded(object sender, RoutedEventArgs e)
        {
            _parentContainer?.RemoveChild(this);
        }

        private void SelectableContainer_Loaded(object sender, RoutedEventArgs e)
        {
            _parentContainer = this.GetFirstAncestorOfType<SelectableContainer>();
            _parentContainer?.AddChild(this);
            IsSelected = false;
            SetEllipseVisibility();
            SetContent();
            if (IsRoot())
            {
                OnSelectionChanged?.Invoke(this, LayoutDocument, DataDocument);
            }
            UpdateSizeMarkers(ContentElement.ActualWidth, ContentElement.ActualHeight);
        }

        private bool IsRoot()
        {
            return _parentContainer == null;
        }

        // TODO THIS WILL CAUSE ERROS WITH CHILD NOT EXISTING
        private void OnContentChanged()
        {
            SetContent();
        }

        private void SetContent()
        {
            if (XLayoutDisplay == null) return;
            XLayoutDisplay.Content = ContentElement;
            ContentElement.IsHitTestVisible = IsSelected;
        }

        #region Selection

        private void CompositeLayoutContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!IsLowestSelected)
            {
                _parentContainer?.SetSelectedContainer(this);
                _parentContainer?.FireSelectionChanged(this);
                IsLowestSelected = true;
                if (IsRoot())
                {
                    FireSelectionChanged(this);
                }
            }
            SetSelectedContainer(null);
            e.Handled = true;
        }

        private void FireSelectionChanged(SelectableContainer selectedContainer)
        {
            OnSelectionChanged?.Invoke(selectedContainer, selectedContainer.LayoutDocument, selectedContainer.DataDocument);
            _parentContainer?.FireSelectionChanged(selectedContainer);
        }

        public void SetSelectedContainer(SelectableContainer layoutContainer)
        {
            if (_selectedLayoutContainer != null)
            {
                _selectedLayoutContainer.IsSelected = false;
                _selectedLayoutContainer.SetSelectedContainer(null); // deselect all children recursively
            }
            _selectedLayoutContainer = layoutContainer;
            if (_selectedLayoutContainer != null)
            {
                _selectedLayoutContainer.IsSelected = true;
                IsLowestSelected = false;
            }
        }


        public SelectableContainer GetSelectedLayout()
        {
            return _selectedLayoutContainer;
        }

        #endregion

        #region Manipulation

        private void InitiateManipulators()
        {
            _draggerList = new List<Ellipse>
            {
                xBottomLeftDragger,
                xTopLeftDragger,
                xBottomRightDragger,
                xTopRightDragger,
                xCenterDragger
            };

            _lineMap = new Dictionary<Ellipse, LinesAndTextBlocks>
            {
                [xTopLeftDragger] = new LinesAndTextBlocks(xTopHLine, xLeftVLine, xTopWidthTextBoxBorder,
                    xLeftHeightTextBoxBorder),
                [xTopRightDragger] = new LinesAndTextBlocks(xTopHLine, xRightVLine, xTopWidthTextBoxBorder,
                    xRightHeightTextBoxBorder),
                [xBottomLeftDragger] = new LinesAndTextBlocks(xBottomHLine, xLeftVLine, xBottomWidthTextBoxBorder,
                    xLeftHeightTextBoxBorder),
                [xBottomRightDragger] = new LinesAndTextBlocks(xBottomHLine, xRightVLine, xBottomWidthTextBoxBorder,
                    xRightHeightTextBoxBorder)
            };

            // manipulation completed
            foreach (var ellipse in _draggerList)
            {
                ellipse.ManipulationCompleted += Manipulator_OnManipulationCompleted;
            }

            // manipulation translated
            var centerManipulator = new ManipulationControls(xCenterDragger);
            centerManipulator.OnManipulatorTranslated += CenterManipulatorOnOnManipulatorTranslated;
            var bottomLeftManipulator = new ManipulationControls(xBottomLeftDragger);
            bottomLeftManipulator.OnManipulatorTranslated += BottomLeftManipulator_OnManipulatorTranslated;
            var bottomRightManipulator = new ManipulationControls(xBottomRightDragger);
            bottomRightManipulator.OnManipulatorTranslated += BottomRightManipulator_OnManipulatorTranslated;
            var topLeftManipulator = new ManipulationControls(xTopLeftDragger);
            topLeftManipulator.OnManipulatorTranslated += TopLeftManipulator_OnManipulatorTranslated;
            var topRightManipulator = new ManipulationControls(xTopRightDragger);
            topRightManipulator.OnManipulatorTranslated += TopRightManipulator_OnManipulatorTranslated;

            // manipulation stated
            foreach (var ellipse in _draggerList)
            {
                ellipse.ManipulationStarted += Manipulator_OnManipulationStarted;
            }
        }

        private void Manipulator_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var snapManager = GetRootSnapManager();

            SnapBehavior snapBehavior = 0;
            var snapPoint = new Point();
            if (sender.Equals(xCenterDragger))
            {
                snapBehavior = SnapBehavior.Center;
                snapPoint.X = ActualWidth / 2;
                snapPoint.Y = ActualHeight / 2;
            }
            else if (sender.Equals(xTopLeftDragger))
            {
                snapBehavior = SnapBehavior.Left | SnapBehavior.Top;

            }
            else if (sender.Equals(xTopRightDragger))
            {
                snapBehavior = SnapBehavior.Top | SnapBehavior.Right;
                snapPoint.X = ActualWidth;

            }
            else if (sender.Equals(xBottomRightDragger))
            {
                snapBehavior = SnapBehavior.Right | SnapBehavior.Bottom;
                snapPoint.X = ActualWidth;
                snapPoint.Y = ActualHeight;

            }
            else if (sender.Equals(xBottomLeftDragger))
            {
                snapBehavior = SnapBehavior.Left | SnapBehavior.Bottom;
                snapPoint.Y = ActualHeight;
            }

            snapManager.SetDraggingContainer(this, snapBehavior, snapPoint);
        }

        private void Manipulator_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            HideManipulatorMeasurements();
            var snapManager = GetRootSnapManager();
            snapManager.DisposeDraggingContainer(this);
        }

        private Point ChangePosition(double deltaX, double deltaY)
        {
            var actualChange = new Point(deltaX, deltaY);
            var positionController = LayoutDocument.GetPositionField();
            var currentPosition = positionController.Data;
            positionController.Data = new Point(currentPosition.X + deltaX, currentPosition.Y + deltaY);
            return actualChange;
        }

        private void SetPosition(double newX, double newY)
        {
            var positionController = LayoutDocument.GetPositionField();
            positionController.Data = new Point(newX, newY);
        }

        private Point GetPosition()
        {
            var positionController = LayoutDocument.GetPositionField();
            return positionController.Data;
        }

        private Point ChangeSize(double deltaWidth, double deltaHeight)
        {
            var actualChange = new Point(0, 0);
            var widthController = LayoutDocument.GetWidthField();
            //TODO: right now this just uses the framework element's minwidth as a boundary for size changes; might want to set minwidth in document later
            if (widthController.Data + deltaWidth > ContentElement.MinWidth || deltaWidth > 0)
            {
                widthController.Data += deltaWidth;
                actualChange.X = deltaWidth;
            }
            var heightController = LayoutDocument.GetHeightField();
            if (heightController.Data + deltaHeight > ContentElement.MinHeight || deltaHeight > 0)
            {
                heightController.Data += deltaHeight;
                actualChange.Y = deltaHeight;
            }
            return actualChange;
        }

        private void SetSize(double newW, double newH)
        {
            var widthController = LayoutDocument.GetWidthField();
            //TODO: right now this just uses the framework element's minwidth as a boundary for size changes; might want to set minwidth in document later
            if (newW > ContentElement.MinWidth || newW > 0)
            {
                widthController.Data = newW;
            }
            var heightController = LayoutDocument.GetHeightField();
            if (newH > ContentElement.MinHeight || newH > 0)
            {
                heightController.Data = newH;
            }
        }

        private Point GetSize()
        {
            var widthController = LayoutDocument.GetWidthField();
            //TODO: right now this just uses the framework element's minwidth as a boundary for size changes; might want to set minwidth in document later
            var heightController = LayoutDocument.GetHeightField();
            return new Point(widthController.Data, heightController.Data);
        }

        private void TopRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xTopRightDragger);
            var sizeChange = ChangeSize(e.Translate.X, -e.Translate.Y);
            var posChange = ChangePosition(0, -sizeChange.Y);
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(e.Translate);
        }

        private void TopLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xTopLeftDragger);
            var sizeChange = ChangeSize(-e.Translate.X, -e.Translate.Y);
            var posChange = ChangePosition(-sizeChange.X, -sizeChange.Y);
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(e.Translate);
        }

        private void BottomRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xBottomRightDragger);
            var sizeChange = ChangeSize(e.Translate.X, e.Translate.Y);
            var posChange = new Point();
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(e.Translate);
        }

        private void BottomLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xBottomLeftDragger);
            var sizeChange = ChangeSize(-e.Translate.X, e.Translate.Y);
            var posChange = ChangePosition(-sizeChange.X, 0);
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(e.Translate);
        }

        private void CenterManipulatorOnOnManipulatorTranslated(TransformGroupData e)
        {
            var posChange = ChangePosition(e.Translate.X, e.Translate.Y);
            var sizeChange = new Point();
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(e.Translate);
        }

        private void SetPressedEllipse(Ellipse ellipse)
        {
            _pressedEllipse = ellipse;
            _lineMap[_pressedEllipse].HLine.Visibility
                = _lineMap[_pressedEllipse].VLine.Visibility
                    = _lineMap[_pressedEllipse].WidthBorder.Visibility
                        = _lineMap[_pressedEllipse].HeightBorder.Visibility
                            = Visibility.Visible;
        }

        private void HideManipulatorMeasurements()
        {
            xTopHLine.Visibility
                = xBottomHLine.Visibility
                    = xLeftVLine.Visibility
                        = xRightVLine.Visibility
                            = xTopWidthTextBoxBorder.Visibility
                                = xBottomWidthTextBoxBorder.Visibility
                                    = xLeftHeightTextBoxBorder.Visibility
                                        = xRightHeightTextBoxBorder.Visibility
                                            = Visibility.Collapsed;
        }

        private void LayoutManipulators()
        {
            var manipulatorWidth = xTopLeftDragger.ActualWidth;
            var manipulatorHeight = xTopLeftDragger.ActualHeight;
            var canvasWidth = xManipulatorCanvas.ActualWidth;
            var canvasHeight = xManipulatorCanvas.ActualHeight;
            Canvas.SetLeft(xTopLeftDragger, -manipulatorWidth);
            Canvas.SetTop(xTopLeftDragger, -manipulatorHeight);
            Canvas.SetLeft(xTopRightDragger, canvasWidth);
            Canvas.SetTop(xTopRightDragger, -manipulatorHeight);
            Canvas.SetLeft(xBottomRightDragger, canvasWidth);
            Canvas.SetTop(xBottomRightDragger, canvasHeight);
            Canvas.SetLeft(xBottomLeftDragger, -manipulatorWidth);
            Canvas.SetTop(xBottomLeftDragger, canvasHeight);
            Canvas.SetLeft(xCenterDragger, (canvasWidth - manipulatorWidth) / 2);
            Canvas.SetTop(xCenterDragger, (canvasHeight - manipulatorHeight) / 2);
        }

        private void XManipulatorCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            LayoutManipulators();
        }

        private void ContentElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newWidth = e.NewSize.Width;
            var newHeight = e.NewSize.Height;
            UpdateSizeMarkers(newWidth, newHeight);
        }

        private void UpdateSizeMarkers(double newWidth, double newHeight)
        {
            if (_pressedEllipse == null) return;
            var linesAndBorders = _lineMap[_pressedEllipse];
            linesAndBorders.HLine.X2 = newWidth;
            linesAndBorders.VLine.Y2 = newHeight;
            if (IsDraggingBottom())
                linesAndBorders.HLine.Y1 = linesAndBorders.HLine.Y2 = XOuterGrid.ActualHeight + 10;
            if (IsDraggingRight())
                linesAndBorders.VLine.X1 = linesAndBorders.VLine.X2 = XOuterGrid.ActualWidth + 10;

            ((TextBlock)linesAndBorders.WidthBorder.Child).Text = "" + (int)newWidth;
            ((TextBlock)linesAndBorders.HeightBorder.Child).Text = "" + (int)newHeight;
            ApplySizeMarkerTransforms(newWidth, newHeight);
        }

        private bool IsDraggingRight()
        {
            return _pressedEllipse == xTopRightDragger || _pressedEllipse == xBottomRightDragger;
        }

        private bool IsDraggingBottom()
        {
            return _pressedEllipse == xBottomLeftDragger || _pressedEllipse == xBottomRightDragger;
        }

        private void ApplySizeMarkerTransforms(double newWidth, double newHeight)
        {
            var linesAndBorders = _lineMap[_pressedEllipse];
            var hTranslate = new TranslateTransform
            {
                X = newWidth / 2 - linesAndBorders.WidthBorder.ActualWidth / 2 + linesAndBorders.HLine.X1 / 2,
                Y = linesAndBorders.HLine.Y1 - linesAndBorders.WidthBorder.ActualHeight / 2
            };
            var vRotate = new RotateTransform
            {
                Angle = 90,
                CenterX = linesAndBorders.HeightBorder.ActualWidth / 2,
                CenterY = linesAndBorders.HeightBorder.ActualHeight / 2
            };
            var vTranslate = new TranslateTransform
            {
                X = linesAndBorders.VLine.X1 - linesAndBorders.HeightBorder.ActualWidth / 2,
                Y = newHeight / 2 - linesAndBorders.HeightBorder.ActualHeight / 2 + linesAndBorders.VLine.Y1 / 2
            };
            var vGroup = new TransformGroup();
            vGroup.Children.Add(vRotate);
            vGroup.Children.Add(vTranslate);
            linesAndBorders.WidthBorder.RenderTransform = hTranslate;
            linesAndBorders.HeightBorder.RenderTransform = vGroup;
        }

        #endregion

        #region guidelinesAndSnapping

        private void AddSnapLine(double lineCoordinate, bool isVertical)
        {
            var line = new Line();
            line.Tag = "GUIDELINE";
            line.Stroke = new SolidColorBrush(Colors.CornflowerBlue);
            line.StrokeThickness = 1;
            line.StrokeDashArray = new DoubleCollection() {2, 2};

            if (isVertical)
            {
                line.X1 = lineCoordinate;
                line.X2 = lineCoordinate;
                line.Y1 = 0;
                line.Y2 = ActualHeight;
            }
            else
            {
                line.X1 = 0;
                line.X2 = ActualWidth;
                line.Y1 = lineCoordinate;
                line.Y2 = lineCoordinate;
            }

            xManipulatorCanvas.Children.Add(line);
        }

        private void ClearSnapLines()
        {
            foreach (var child in xManipulatorCanvas.Children.ToArray())
            {
                if ((child as Line)?.Tag as string == "GUIDELINE")
                {
                    xManipulatorCanvas.Children.Remove(child);
                }
            }
        }

        private SelectableContainer GetRoot()
        {
            return IsRoot() ? this : _parentContainer.GetRoot();
        }

        private List<SelectableContainer> GetAllChildren()
        {
            var allChildren = new List<SelectableContainer>();

            // add our children
            allChildren.AddRange(_childContainers);

            // add our children's children
            foreach (var child in _childContainers)
            {
                allChildren.AddRange(child.GetAllChildren());
            }

            // finally add ourself if we are the root
            if (IsRoot())
                allChildren.Add(this);

            return allChildren;
        }

        private void AddChild(SelectableContainer newChild)
        {
            _childContainers.Add(newChild);
        }

        private void RemoveChild(SelectableContainer oldChild)
        {
            _childContainers.Remove(oldChild);
        }

        private RootSnapManager GetRootSnapManager()
        {
            var root = GetRoot();
            return root._rootSnapManager ?? (root._rootSnapManager = new RootSnapManager(root));
        }

        [Flags]
        private enum SnapBehavior
        {
            Left = 0x1,
            Top = 0x2,
            Right = 0x4,
            Bottom = 0x8,
            Center = 0x10
        }

        private class RootSnapManager
        {
            private const double Snapoffset = 5;

            private readonly SelectableContainer _rootContainer;
            private SelectableContainer _draggingContainer;
            private SnapBehavior _dragSnapBehavior;
            private Point _actual;
            private Point _start;
            private Point _startSize;

            public RootSnapManager(SelectableContainer rootContainer)
            {
                _rootContainer = rootContainer;
            }

            // manipulation started
            public void SetDraggingContainer(SelectableContainer draggingContainer, SnapBehavior snapBehavior, Point actual)
            {
                _draggingContainer = draggingContainer;
                _dragSnapBehavior = snapBehavior;

                var transform = GetChildToRootTransform(_draggingContainer);
                _actual = transform.TransformPoint(actual);
                _start = new Point(_actual.X, _actual.Y);
                _startSize = _draggingContainer.GetSize();
            }

            // manipulation completed
            public void DisposeDraggingContainer(SelectableContainer draggingContainer)
            {
                _rootContainer.ClearSnapLines();
            }

            // manipulation translate
            public void UpdateDraggingContainer(Point translate)
            {
                _rootContainer.ClearSnapLines();
                var allContainers = _rootContainer.GetAllChildren();
                var allLines = CalculateLines(allContainers);

                _actual.X += translate.X;
                _actual.Y += translate.Y;

                var newPosition = _draggingContainer.GetPosition();
                var newSize = _draggingContainer.GetSize();

                Point toBeRenderPosition;
                if ((_dragSnapBehavior & SnapBehavior.Center) == SnapBehavior.Center)
                {
                    var topLeft = new Point(_actual.X - newSize.X / 2, _actual.Y - newSize.Y / 2);
                    var snappedTopLeft = SnapActual(allLines, topLeft);
                    var bottomRight = new Point(snappedTopLeft.X + newSize.X, snappedTopLeft.Y + newSize.Y);
                    var snappedBottomRight = SnapActual(allLines, bottomRight);
                    var center = new Point(snappedBottomRight.X - newSize.X / 2, snappedBottomRight.Y - newSize.Y / 2);
                    toBeRenderPosition = SnapActual(allLines, center);
                }
                else
                {
                    toBeRenderPosition =  SnapActual(allLines, _actual);
                }


                if ((_dragSnapBehavior & SnapBehavior.Left) == SnapBehavior.Left)
                {
                    newPosition.X = toBeRenderPosition.X;
                    newSize.X =
                        _startSize.X + _start.X - toBeRenderPosition.X;
                }

                if ((_dragSnapBehavior & SnapBehavior.Top) == SnapBehavior.Top)
                {
                    newPosition.Y = toBeRenderPosition.Y;
                    newSize.Y =
                        _startSize.Y + _start.Y - toBeRenderPosition.Y;
                }

                if ((_dragSnapBehavior & SnapBehavior.Right) == SnapBehavior.Right)
                {
                    newSize.X = _startSize.X + toBeRenderPosition.X - _start.X;
                }

                if ((_dragSnapBehavior & SnapBehavior.Bottom) == SnapBehavior.Bottom)
                {
                    newSize.Y = _startSize.Y + toBeRenderPosition.Y - _start.Y;
                }

                if ((_dragSnapBehavior & SnapBehavior.Center) == SnapBehavior.Center)
                {
                    newPosition.X = toBeRenderPosition.X - _draggingContainer.ActualWidth / 2;
                    newPosition.Y = toBeRenderPosition.Y - _draggingContainer.ActualHeight / 2;
                }

                _draggingContainer.SetPosition(newPosition.X, newPosition.Y);
                _draggingContainer.SetSize(newSize.X, newSize.Y);

                DrawSnapLines(toBeRenderPosition, allLines);
                if ((_dragSnapBehavior & SnapBehavior.Center) == SnapBehavior.Center)
                {
                    var upperLeft = new Point(toBeRenderPosition.X - newSize.X / 2, toBeRenderPosition.Y - newSize.Y / 2);
                    DrawSnapLines(upperLeft, allLines);
                    var lowerRight = new Point(toBeRenderPosition.X + newSize.X / 2, toBeRenderPosition.Y + newSize.Y / 2);
                    DrawSnapLines(lowerRight, allLines);
                }
            }

            private void DrawSnapLines(Point toBeRenderPosition, Lines allLines)
            {
                if (allLines.VerticalLines.Contains(toBeRenderPosition.X))
                {
                    _rootContainer.AddSnapLine(toBeRenderPosition.X, true);
                }

                if (allLines.HorizontalLines.Contains(toBeRenderPosition.Y))
                {
                    _rootContainer.AddSnapLine(toBeRenderPosition.Y, false);
                }
            }

            private Point SnapActual(Lines allLines, Point actual)
            {
                var minVert = CalculateMinSnap(allLines.VerticalLines, actual.X);
                var minHorz = CalculateMinSnap(allLines.HorizontalLines, actual.Y);

                var x = minVert ?? actual.X;
                var y = minHorz ?? actual.Y;

                return new Point(x, y);
            }

            private double? CalculateMinSnap(HashSet<double> allPossibleSnaps, double coordinateToCheck)
            {
                var minSnap = double.PositiveInfinity;
                var minDistance = double.PositiveInfinity;
                foreach (var snap in allPossibleSnaps)
                {
                    var distance = Math.Abs(snap - coordinateToCheck);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minSnap = snap;
                    }
                }

                if (Math.Abs(minSnap - coordinateToCheck) < Snapoffset)
                {
                    return minSnap;
                }
                return null;
            }


            /// <summary>
            /// Calculates all the possible line in root container coordinates, based on each of the container's rendered
            /// widths and heights. Excludes the dragging container
            /// </summary>
            private Lines CalculateLines(List<SelectableContainer> selectableContainers)
            {
                var lines = new Lines();

                foreach (var container in selectableContainers)
                {
                    if (container.Equals(_draggingContainer)) continue;

                    var transform = GetChildToRootTransform(container);

                    var upperLeftLines = transform.TransformPoint(new Point());
                    lines.HorizontalLines.Add(upperLeftLines.Y);
                    lines.VerticalLines.Add(upperLeftLines.X);

                    var lowRightLines = transform.TransformPoint(new Point(container.ActualWidth, container.ActualHeight));
                    lines.HorizontalLines.Add(lowRightLines.Y);
                    lines.VerticalLines.Add(lowRightLines.X);

                    var centerLines = transform.TransformPoint(new Point(container.ActualWidth / 2, container.ActualHeight / 2));
                    lines.HorizontalLines.Add(centerLines.Y);
                    lines.VerticalLines.Add(centerLines.X);
                }

                return lines;
            }

            private GeneralTransform GetChildToRootTransform(SelectableContainer child)
            {
                return child.TransformToVisual(_rootContainer);
            }

            private class Lines
            {
                public HashSet<double> VerticalLines;
                public HashSet<double> HorizontalLines;

                public Lines()
                {
                    VerticalLines = new HashSet<double>();
                    HorizontalLines = new HashSet<double>();
                }
            }
        }

        #endregion

    }
}
