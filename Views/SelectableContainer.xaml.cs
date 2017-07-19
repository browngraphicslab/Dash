using System;
using System.Collections.Generic;
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
using Windows.UI;
using Dash.ViewModels;

namespace Dash
{
    public partial class SelectableContainer : UserControl
    {

        private SelectableContainer _selectedLayoutContainer;
        private SelectableContainer _parentContainer;
        private bool _isSelected;
        private FrameworkElement _contentElement;

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
                }
            }
        }

        public SelectableContainer(FrameworkElement contentElement)
        {
            this.InitializeComponent();

            ContentElement = contentElement;

            Loaded += SelectableContainer_Loaded;
            Tapped += CompositeLayoutContainer_Tapped;

        }

        private void SelectableContainer_Loaded(object sender, RoutedEventArgs e)
        {
            _parentContainer = this.GetFirstAncestorOfType<SelectableContainer>();
            IsSelected = false;

            SetContent();
        }

        private void CompositeLayoutContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!IsSelected)
            {
                if (_parentContainer != null) _parentContainer.SetSelectedContainer(this);
            }
            SetSelectedContainer(null);
            e.Handled = true;
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

        public void SetSelectedContainer(SelectableContainer layoutContainer)
        {
            if (_selectedLayoutContainer != null) _selectedLayoutContainer.IsSelected = false;
            _selectedLayoutContainer = layoutContainer;
            if (_selectedLayoutContainer != null) _selectedLayoutContainer.IsSelected = true;


        }

        public SelectableContainer GetSelectedLayout()
        {
            return _selectedLayoutContainer;
        }
    }
}
