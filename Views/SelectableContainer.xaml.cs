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
using Dash.ViewModels;

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
                    XGrid.BorderThickness = new Thickness(3);
                }
                else
                {
                    XGrid.BorderThickness = new Thickness(1);
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
            Tapped += CompositeLayoutContainer_Tapped;
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
            snapManager.SetDraggingContainer(this);
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

        private void TopRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xTopRightDragger);
            var sizeChange = ChangeSize(e.Translate.X, -e.Translate.Y);
            var posChange = ChangePosition(0, -sizeChange.Y);
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(sizeChange, posChange);
        }

        private void TopLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xTopLeftDragger);
            var sizeChange = ChangeSize(-e.Translate.X, -e.Translate.Y);
            var posChange = ChangePosition(-sizeChange.X, -sizeChange.Y);
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(sizeChange, posChange);
        }

        private void BottomRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xBottomRightDragger);
            var sizeChange = ChangeSize(e.Translate.X, e.Translate.Y);
            var posChange = new Point();
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(sizeChange, posChange);
        }

        private void BottomLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xBottomLeftDragger);
            var sizeChange = ChangeSize(-e.Translate.X, e.Translate.Y);
            var posChange = ChangePosition(-sizeChange.X, 0);
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(sizeChange, posChange);
        }

        private void CenterManipulatorOnOnManipulatorTranslated(TransformGroupData delta)
        {
            var posChange = ChangePosition(delta.Translate.X, delta.Translate.Y);
            var sizeChange = new Point();
            var snapManager = GetRootSnapManager();
            snapManager.UpdateDraggingContainer(sizeChange, posChange, isTranslate: true);
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

        private SelectableContainer GetRoot()
        {
            return IsRoot() ? this : _parentContainer.GetRoot();
        }

        private List<SelectableContainer> GetAllChildrenRecursively()
        {
            var allChildren = new List<SelectableContainer>();

            // add our children's children
            foreach (var directChild in _childContainers)
            {
                allChildren.AddRange(directChild.GetAllChildrenRecursively());
            }
            // then add our direct children
            allChildren.AddRange(_childContainers);

            // finally add ourself if we are the root
            if (IsRoot()) allChildren.Add(this);

            return allChildren;
        }

        private void AddChild(SelectableContainer newChild)
        {
            _childContainers.Add(newChild);
        }

        private RootSnapManager GetRootSnapManager()
        {
            var root = GetRoot();
            return root._rootSnapManager ?? (_rootSnapManager = new RootSnapManager(root));
        }

        private class RootSnapManager
        {
            private const double Snapoffset = 15;

            private readonly SelectableContainer _rootContainer;
            private SelectableContainer _draggingContainer;
            private Snap _currentSnaps;

            public RootSnapManager(SelectableContainer rootContainer)
            {
                _rootContainer = rootContainer;
            }

            public void SetDraggingContainer(SelectableContainer draggingContainer)
            {
                _draggingContainer = draggingContainer;
            }

            public void DisposeDraggingContainer(SelectableContainer draggingContainer)
            {
                _draggingContainer = null;
            }

            public void UpdateDraggingContainer(Point sizeChange, Point posChange, bool isTranslate = false)
            {
                var allContainers = _rootContainer.GetAllChildrenRecursively();
                var allLines = CalculateLines(allContainers);
                var previousSnaps = _currentSnaps;
                _currentSnaps = CalculateSnaps(allLines);

                if (_currentSnaps.Left.HasValue)
                {
                }

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
                }

                return lines;
            }

            private Snap CalculateSnaps(Lines allLines)
            {
                var dragUpperLeft = DraggingContainerGhostUpperLeft();
                var dragLowerRight = DraggingContainerGhostLowerRight();

                var snap = new Snap(Snapoffset);

                snap.CalculateLeft(allLines.VerticalLines, dragUpperLeft.X);
                snap.CalculateTop(allLines.HorizontalLines, dragUpperLeft.Y);
                snap.CalculateRight(allLines.VerticalLines, dragLowerRight.X);
                snap.CalculateBottom(allLines.HorizontalLines, dragLowerRight.Y);

                return snap;
            }

            /// <summary>
            /// Gets the point of the dragging container with ghosted positioning and sizing taken account of
            /// </summary>
            private Point DraggingContainerGhostLowerRight()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Gets the point of the dragging container with ghosted positioning and sizing taken account of
            /// </summary>
            private Point DraggingContainerGhostUpperLeft()
            {
                throw new NotImplementedException();
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

            private class Snap
            {
                public double? Left;
                public double? Top;
                public double? Right;
                public double? Bottom;

                private readonly double _snapoffset;

                public Snap(double snapoffset)
                {
                    _snapoffset = snapoffset;
                }

                public bool HasLeftSnap => Left.HasValue;
                public bool HasTopSnap => Top.HasValue;
                public bool HasRightSnap => Right.HasValue;
                public bool HasBottomSnap => Bottom.HasValue;

                public void CalculateLeft(HashSet<double> allVerticalLines, double leftX)
                {
                    Left = CalculateMinSnap(allVerticalLines, leftX);
                }

                public void CalculateTop(HashSet<double> allHorizontalLines, double leftY)
                {
                    Top = CalculateMinSnap(allHorizontalLines, leftY);
                }

                public void CalculateRight(HashSet<double> allVerticalinLines, double rightX)
                {
                    Right = CalculateMinSnap(allVerticalinLines, rightX);
                }

                public void CalculateBottom(HashSet<double> allHorizontalLines, double rightY)
                {
                    Bottom = CalculateMinSnap(allHorizontalLines, rightY);
                }

                private double? CalculateMinSnap(HashSet<double> allPossibleSnaps, double coordinateToCheck)
                {
                    var minSnap = allPossibleSnaps.Min(possibleSnap => Math.Abs(possibleSnap - coordinateToCheck));
                    if (Math.Abs(minSnap - coordinateToCheck) < _snapoffset)
                    {
                        return minSnap;
                    }
                    return null;
                }
            }
        }

        //private void RootSelectableContainerOnMovedOrResized(SelectableContainer sender, bool isResize)
        //{
        //    _rootSelectableContainer.ClearSnapLines();
        //    var selectableContainers = _rootSelectableContainer.GetAllChildrenRecursively();
        //    var lines = CalculateLines(sender, selectableContainers);
        //    var snapLines = CalculateSnap(sender, lines);

        //    if (snapLines.HorizontalLines.Count == 0 && snapLines.VerticalLines.Count == 0)
        //    {
        //        if (_wasSnapped)
        //        {
        //            sender.ChangePosition(sender.Ghost.PositionDeltaFromReal.X, sender.Ghost.PositionDeltaFromReal.Y);
        //            sender.ChangeSize(sender.Ghost.SizeDeltaFromReal.X, sender.Ghost.SizeDeltaFromReal.Y);
        //        }
        //        _wasSnapped = false;
        //        sender.Ghost.ResetGhost();
        //    }
        //    else
        //    {
        //        DrawSnapLines(snapLines);
        //        PerformSnap(sender, snapLines, isResize);
        //        _wasSnapped = true;
        //    }
        //}

        //private void DrawSnapLines(Lines snapLines)
        //{
        //    foreach (var verticalLine in snapLines.VerticalLines)
        //    {
        //        _rootSelectableContainer.AddVerticalSnapLine(verticalLine);
        //    }

        //    foreach (var horizontalLine in snapLines.HorizontalLines)
        //    {
        //        _rootSelectableContainer.AddHorizontalSnapLine(horizontalLine);
        //    }
        //}

        //private void PerformSnap(SelectableContainer sender, Lines snapLines, bool isResize)
        //{
        //    var upperLeftLines = sender.Ghost.RealUpperLeft;
        //    var lowRightLines = sender.Ghost.RealLowerRight;

        //    foreach (var horizontalLine in snapLines.HorizontalLines)
        //    {
        //        // snap upper left to horizontal
        //        if (Math.Abs(upperLeftLines.Y - horizontalLine) < _snapOffset)
        //        {
        //            sender.ChangeSize(0, upperLeftLines.Y - horizontalLine, false);
        //            sender.ChangePosition(0, -(upperLeftLines.Y - horizontalLine), false);
        //        }

        //        // snap lower right to horizontal
        //        if (Math.Abs(lowRightLines.Y - horizontalLine) < _snapOffset)
        //        {
        //            sender.ChangeSize(0, -(lowRightLines.Y - horizontalLine), false);
        //        }
        //    }

        //    foreach (var verticalLine in snapLines.VerticalLines)
        //    {
        //        // snap upper left to vertical
        //        if (Math.Abs(upperLeftLines.X - verticalLine) < _snapOffset)
        //        {
        //            sender.ChangeSize(upperLeftLines.X - verticalLine, 0, false);
        //            sender.ChangePosition(-(upperLeftLines.X - verticalLine), 0, false);

        //        }

        //        // snap lower right to vertical
        //        if (Math.Abs(lowRightLines.X - verticalLine) < _snapOffset)
        //        {
        //            sender.ChangeSize(-(lowRightLines.X - verticalLine), 0, false);
        //        }
        //    }
        //}

        //private Lines CalculateSnap(SelectableContainer sender, Lines lines)
        //{
        //    var upperLeftLines = sender.Ghost.GhostUpperLeft;
        //    var lowRightLines = sender.Ghost.GhostLowerRight;

        //    var snapLines = new Lines();

        //    foreach (var horizontalLine in lines.HorizontalLines)
        //    {
        //        if (Math.Abs(upperLeftLines.Y - horizontalLine) < _snapOffset ||
        //            Math.Abs(lowRightLines.Y - horizontalLine) < _snapOffset)
        //        {
        //            snapLines.HorizontalLines.Add(horizontalLine);
        //        }
        //    }

        //    foreach (var verticalLine in lines.VerticalLines)
        //    {
        //        if (Math.Abs(upperLeftLines.X - verticalLine) < _snapOffset ||
        //            Math.Abs(lowRightLines.X - verticalLine) < _snapOffset)
        //        {
        //            snapLines.VerticalLines.Add(verticalLine);
        //        }
        //    }

        //    return snapLines;



        //}

        //private Lines CalculateLines(SelectableContainer sender, List<SelectableContainer> selectableContainers)
        //{
        //    var lines = new Lines();

        //    foreach (var container in selectableContainers)
        //    {
        //        if (container.Equals(sender)) continue;

        //        var transform = container.TransformToVisual(_rootSelectableContainer);

        //        var upperLeftLines = transform.TransformPoint(new Point());
        //        lines.HorizontalLines.Add(upperLeftLines.Y);
        //        lines.VerticalLines.Add(upperLeftLines.X);

        //        var lowRightLines = transform.TransformPoint(new Point(container.ActualWidth, container.ActualHeight));
        //        lines.HorizontalLines.Add(lowRightLines.Y);
        //        lines.VerticalLines.Add(lowRightLines.X);
        //    }

        //    return lines;
        //}

        //private class Lines
        //{
        //    public HashSet<double> VerticalLines;
        //    public HashSet<double> HorizontalLines;

        //    public Lines()
        //    {
        //        VerticalLines = new HashSet<double>();
        //        HorizontalLines = new HashSet<double>();
        //    }
        //}



        #endregion

    }
}
