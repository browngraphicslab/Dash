using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
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
    public sealed partial class CollectionListView : UserControl
    {
        public ICollectionViewModel ViewModel { get; private set; }

        public CollectionListView(ICollectionViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            HListView.DragItemsStarting += viewModel.xGridView_OnDragItemsStarting;
            HListView.DragItemsCompleted += viewModel.xGridView_OnDragItemsCompleted;
            DataContextChanged += OnDataContextChanged;
            Binding selectionBinding = new Binding
            {
                Source = viewModel,
                Path = new PropertyPath(nameof(viewModel.ItemSelectionMode)),
                Mode = BindingMode.OneWay,
            };
            HListView.SetBinding(ListView.SelectionModeProperty, selectionBinding);
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as CollectionViewModel;
            HListView.SelectionChanged += vm.SelectionChanged;
        }

        private void HListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        #region DragAndDrop

        private void CollectionViewOnDragOver(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragOver(sender, e);
        }

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDrop(sender, e);
        }

        #endregion
    }
}
