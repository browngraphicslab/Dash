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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionGridView : UserControl
    {
        public CollectionGridView(CollectionView view)
        {
            this.InitializeComponent();
            xGridView.DragItemsStarting += view.xGridView_OnDragItemsStarting;
            xGridView.DragItemsCompleted += view.xGridView_OnDragItemsCompleted;
            DataContextChanged += OnDataContextChanged;
            Binding selectionBinding = new Binding
            {
                Source = view.ViewModel,
                Path = new PropertyPath(nameof(view.ViewModel.ItemSelectionMode)),
                Mode = BindingMode.OneWay,
            };
            xGridView.SetBinding(GridView.SelectionModeProperty, selectionBinding);

        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            CollectionViewModel vm = DataContext as CollectionViewModel;
            xGridView.SelectionChanged += vm.SelectionChanged;
        }
    }
}
