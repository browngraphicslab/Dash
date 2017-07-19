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

namespace Dash.Views
{
    public partial class SelectableContainer : UserControl
    {

        private SelectableContainer _selectedLayoutContainer;
        private SelectableContainer _parentContainer;
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                XLayoutDisplay.IsHitTestVisible = value;
                if (value) XGrid.BorderThickness = new Thickness(3);
                else XGrid.BorderThickness = new Thickness(1);
            }
        }

        protected SelectableContainer()
        {
            _parentContainer = this.GetFirstAncestorOfType<SelectableContainer>();
            Tapped += CompositeLayoutContainer_Tapped;
            IsSelected = false;
        }

        private void CompositeLayoutContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!IsSelected)
            {
                _parentContainer.SetSelectedLayout(this);
            }
            SetSelectedLayout(null);
            e.Handled = true;
        }

        public void SetSelectedLayout(SelectableContainer layoutContainer)
        {
            _selectedLayoutContainer.IsSelected = false;
            _selectedLayoutContainer = layoutContainer;
            if (layoutContainer != null)
            {
                layoutContainer.IsSelected = true;
            }
            
        }

        public SelectableContainer GetSelectedLayout()
        {
            return _selectedLayoutContainer;
        }

        public void SetContent(FrameworkElement content)
        {
            XLayoutDisplay.Content = content;
            content.DataContext = DataContext;
        }
    }
}
