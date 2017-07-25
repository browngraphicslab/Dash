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
        public SelectableContainer ParentContainer;
        private bool _isSelected;
        private FrameworkElement _contentElement;
        private List<Ellipse> _draggerList;
        private Dictionary<Ellipse, LinesAndTextBlocks> _lineMap;
        private Ellipse _pressedEllipse;

        public readonly DocumentController LayoutDocument;
        public readonly DocumentController DataDocument;
        private ManipulationControls _manipulator;
        private bool _isLowestSelected;

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
            InitializeEllipsePointerHandling();
            ContentElement = contentElement;
            ContentElement.SizeChanged += ContentElement_SizeChanged;
            ContentElement.Loaded += ContentElement_Loaded;
            LayoutDocument = layoutDocument;
            DataDocument = dataDocument;

            RenderTransform = new TranslateTransform();

            Loaded += SelectableContainer_Loaded;
            Tapped += CompositeLayoutContainer_Tapped;
        }

        private void ContentElement_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSizeMarkers(ContentElement.ActualWidth, ContentElement.ActualHeight);
        }

        private void SelectableContainer_Loaded(object sender, RoutedEventArgs e)
        {
            ParentContainer = this.GetFirstAncestorOfType<SelectableContainer>();
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
            return ParentContainer == null;
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
                ParentContainer?.SetSelectedContainer(this);
                ParentContainer?.FireSelectionChanged(this);
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
            ParentContainer?.FireSelectionChanged(selectedContainer);
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
        }

        private void InitializeEllipsePointerHandling()
        {
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
            xTopLeftDragger.ManipulationCompleted += Ellipse_ManipulationCompleted;
            xTopRightDragger.ManipulationCompleted += Ellipse_ManipulationCompleted;
            xBottomLeftDragger.ManipulationCompleted += Ellipse_ManipulationCompleted;
            xBottomRightDragger.ManipulationCompleted += Ellipse_ManipulationCompleted;
        }

        private void Ellipse_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            SetPressedEllipse(null);
        }

        private void ChangePosition(double deltaX, double deltaY)
        {
            var positionController = LayoutDocument.GetPositionField();
            var currentPosition = positionController.Data;
            positionController.Data = new Point(currentPosition.X + deltaX, currentPosition.Y + deltaY);
        }

        private Point ChangeSize(double deltaWidth, double deltaHeight)
        {
            Point actualChange = new Point(0, 0);
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
            ChangePosition(0, -sizeChange.Y);           
        }

        private void TopLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xTopLeftDragger);
            var sizeChange = ChangeSize(-e.Translate.X, -e.Translate.Y);
            ChangePosition(-sizeChange.X, -sizeChange.Y);           
        }

        private void BottomRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xBottomRightDragger);
            ChangeSize(e.Translate.X, e.Translate.Y);           
        }

        private void BottomLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            SetPressedEllipse(xBottomLeftDragger);
            var sizeChange = ChangeSize(-e.Translate.X, e.Translate.Y);
            ChangePosition(-sizeChange.X, 0);        
        }

        private void SetPressedEllipse(Ellipse ellipse)
        {
            _pressedEllipse = ellipse;
            if (ellipse == null)
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
                return;
            }
            _lineMap[_pressedEllipse].HLine.Visibility
                = _lineMap[_pressedEllipse].VLine.Visibility
                    = _lineMap[_pressedEllipse].WidthBorder.Visibility
                        = _lineMap[_pressedEllipse].HeightBorder.Visibility
                            = Visibility.Visible;
        }

        private void CenterManipulatorOnOnManipulatorTranslated(TransformGroupData delta)
        {
            ChangePosition(delta.Translate.X, delta.Translate.Y);
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

        #endregion

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
            if (DraggingBottom())
                linesAndBorders.HLine.Y1 = linesAndBorders.HLine.Y2 = XOuterGrid.ActualHeight + 10;
            if (DraggingRight())
                linesAndBorders.VLine.X1 = linesAndBorders.VLine.X2 = XOuterGrid.ActualWidth + 10;

            ((TextBlock)linesAndBorders.WidthBorder.Child).Text = "" + (int)newWidth;
            ((TextBlock)linesAndBorders.HeightBorder.Child).Text = "" + (int)newHeight;
            ApplySizeMarkerTransforms(newWidth, newHeight);
        }

        private bool DraggingRight()
        {
            return _pressedEllipse == xTopRightDragger || _pressedEllipse == xBottomRightDragger;
        }

        private bool DraggingBottom()
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
    }
}
