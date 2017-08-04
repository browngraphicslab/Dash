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
    public sealed partial class CollectionListView : SelectionElement, ICollectionView
    {
        public ICollectionViewModel ViewModel { get; private set; }

        public CollectionListView(ICollectionViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            xListView.DragItemsStarting += ViewModel.xGridView_OnDragItemsStarting;
            xListView.DragItemsCompleted += ViewModel.xGridView_OnDragItemsCompleted;
            xListView.SelectionChanged += XListView_SelectionChanged;
            this.Unloaded += CollectionListView_Unloaded;
        }

        private void CollectionListView_Unloaded(object sender, RoutedEventArgs e)
        {
            xListView.DragItemsStarting -= ViewModel.xGridView_OnDragItemsStarting;
            xListView.DragItemsCompleted -= ViewModel.xGridView_OnDragItemsCompleted;
            xListView.SelectionChanged -= XListView_SelectionChanged;
            this.Unloaded -= CollectionListView_Unloaded;
        }

        #region ItemSelection

        private void XListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {          
            ViewModel.SelectionGroup.Clear();
            ViewModel.SelectionGroup.AddRange(xListView.SelectedItems.Cast<DocumentViewModel>());
        }

        public void ToggleSelectAllItems()
        {
            var isAllItemsSelected = xListView.SelectedItems.Count == ViewModel.DocumentViewModels.Count;
            if (!isAllItemsSelected)
            {
                xListView.SelectAll();
            }
            else
            {
                xListView.SelectedItems.Clear();
            }
        }

        #endregion


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

        #region Activation

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (ViewModel.IsInterfaceBuilder)
                return;

            OnSelected();

        }

        #endregion
    }
}
