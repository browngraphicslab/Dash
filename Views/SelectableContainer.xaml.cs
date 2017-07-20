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
using Windows.Security.Cryptography.Core;
using Windows.UI;
using Windows.UI.Xaml.Shapes;
using Dash.ViewModels;

namespace Dash
{
    public partial class SelectableContainer : UserControl
    {
        public delegate void OnSelectionChangedHandler(SelectableContainer sender, DocumentController layoutDocument);

        public event OnSelectionChangedHandler OnSelectionChanged;


        private SelectableContainer _selectedLayoutContainer;
        private SelectableContainer _parentContainer;
        private bool _isSelected;
        private FrameworkElement _contentElement;
        private List<Ellipse> _draggerList;

        public DocumentController LayoutDocument;
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
                _isSelected = _parentContainer == null ? true : value;
                ContentElement.IsHitTestVisible = value;
                if (_isSelected)
                {
                    XGrid.BorderThickness = new Thickness(3);
                } else
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
                if (value)
                {
                    foreach (var ellipse in _draggerList)
                    {
                        ellipse.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    foreach (var ellipse in _draggerList)
                    {
                        ellipse.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        public SelectableContainer(FrameworkElement contentElement, DocumentController layoutDocument)
        {
            this.InitializeComponent();
            this.InitiateManipulators();
            ContentElement = contentElement;
            LayoutDocument = layoutDocument;

            RenderTransform = new TranslateTransform();

            Loaded += SelectableContainer_Loaded;
            Tapped += CompositeLayoutContainer_Tapped;
        }

       
        private void SelectableContainer_Loaded(object sender, RoutedEventArgs e)
        {
            _parentContainer = this.GetFirstAncestorOfType<SelectableContainer>();
            IsSelected = false;
            SetContent();
            if (_parentContainer == null)
            {
                OnSelectionChanged?.Invoke(this, LayoutDocument);
            }
        }

        // TODO THIS WILL CAUSE ERROS WITH CHILD NOT EXISTING
        private void OnContentChanged()
        {
            SetContent();
        }

        private void SetContent()
        {
            if (XLayoutDisplay != null)
            {
                XLayoutDisplay.Content = ContentElement;
                ContentElement.IsHitTestVisible = IsSelected;
            }
        }

        #region Selection

        private void CompositeLayoutContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!IsLowestSelected)
            {
                _parentContainer?.SetSelectedContainer(this);
                _parentContainer?.FireSelectionChanged(this);
                IsLowestSelected = true;
                if (_parentContainer == null)
                {
                    OnSelectionChanged?.Invoke(this, LayoutDocument);
                }
            }
            SetSelectedContainer(null);
            e.Handled = true;
        }

        private void FireSelectionChanged(SelectableContainer selectedContainer)
        {
            OnSelectionChanged?.Invoke(selectedContainer, selectedContainer.LayoutDocument);
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

        private void ChangePosition(double deltaX, double deltaY)
        {
            var positionController = LayoutDocument.GetPositionField();
            var currentPosition = positionController.Data;
            positionController.Data = new Point(currentPosition.X + deltaX, currentPosition.Y + deltaY);
        }

        private Point ChangeSize(double deltaWidth, double deltaHeight)
        {
            Point actualChange = new Point(0,0);
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
            var sizeChange = ChangeSize(e.Translate.X, -e.Translate.Y);
            ChangePosition(0, -sizeChange.Y);
        }

        private void TopLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            var sizeChange = ChangeSize(-e.Translate.X, -e.Translate.Y);
            ChangePosition(-sizeChange.X, -sizeChange.Y);
        }

        private void BottomRightManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            ChangeSize(e.Translate.X, e.Translate.Y);
        }

        private void BottomLeftManipulator_OnManipulatorTranslated(TransformGroupData e)
        {
            var sizeChange = ChangeSize(-e.Translate.X, e.Translate.Y);
            ChangePosition(-sizeChange.X, 0);
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
    }
}
