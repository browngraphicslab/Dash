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
        public DocumentController LayoutDocument;

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
                    xBottomLeftDragger.Visibility = Visibility.Visible;
                    xTopLeftDragger.Visibility = Visibility.Visible;
                    xBottomRightDragger.Visibility = Visibility.Visible;
                    xTopRightDragger.Visibility = Visibility.Visible;
                    xCenterDragger.Visibility = Visibility.Visible;
                } else
                {
                    XGrid.BorderThickness = new Thickness(1);
                    xBottomLeftDragger.Visibility = Visibility.Collapsed;
                    xTopLeftDragger.Visibility = Visibility.Collapsed;
                    xBottomRightDragger.Visibility = Visibility.Collapsed;
                    xTopRightDragger.Visibility = Visibility.Collapsed;
                    xCenterDragger.Visibility = Visibility.Collapsed;
                }
            }
        }

        public SelectableContainer(FrameworkElement contentElement, DocumentController layoutDocument)
        {
            ContentElement = contentElement;
            LayoutDocument = layoutDocument;
            this.InitializeComponent();

            RenderTransform = new TranslateTransform();

            Loaded += SelectableContainer_Loaded;
            Tapped += CompositeLayoutContainer_Tapped;
        }

        private void SelectableContainer_Loaded(object sender, RoutedEventArgs e)
        {
            _parentContainer = this.GetFirstAncestorOfType<SelectableContainer>();
            IsSelected = false;

            SetContent();
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
            if (!IsSelected)
            {
                _parentContainer?.SetSelectedContainer(this);
                _parentContainer?.FireSelectionChanged(this);
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
            if (_selectedLayoutContainer != null) _selectedLayoutContainer.IsSelected = true;
        }


        public SelectableContainer GetSelectedLayout()
        {
            return _selectedLayoutContainer;
        }

        #endregion

        #region Manipulation

        private void ChangePosition(double deltaX, double deltaY)
        {
            var positionController = LayoutDocument.GetPositionField();
            var currentPosition = positionController.Data;
            positionController.Data = new Point(currentPosition.X + deltaX, currentPosition.Y + deltaY);
        }

        private void ChangeSize(double deltaWidth, double deltaHeight)
        {
            var widthController = LayoutDocument.GetWidthField();
            widthController.Data += deltaWidth;
            var heightController = LayoutDocument.GetHeightField();
            heightController.Data += deltaHeight;
        }

        private void XBottomLeftDragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ChangeSize(-e.Delta.Translation.X, e.Delta.Translation.Y);
            ChangePosition(e.Delta.Translation.X, 0);
            e.Handled = true;
        }


        private void XBottomRightDragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ChangeSize(e.Delta.Translation.X, e.Delta.Translation.Y);
            e.Handled = true;
        }

        private void XTopLeftDragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ChangeSize(-e.Delta.Translation.X, -e.Delta.Translation.Y);
            ChangePosition(e.Delta.Translation.X, e.Delta.Translation.Y);
            e.Handled = true;
        }

        private void XTopRightDragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ChangeSize(e.Delta.Translation.X, -e.Delta.Translation.Y);
            ChangePosition(0, e.Delta.Translation.Y);
            e.Handled = true;
        }

        private void XCenterDragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ChangePosition(e.Delta.Translation.X, e.Delta.Translation.Y);
            e.Handled = true;
        }

        #endregion
    }
}
