using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed partial class CollectionGridView : SelectionElement, ICollectionView
    {
        public BaseCollectionViewModel ViewModel { get; private set; }
        //private ScrollViewer _scrollViewer;

        public CollectionGridView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += CollectionGridView_Unloaded;
        }

        public CollectionGridView(BaseCollectionViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
        private void XGridView_OnLoaded(object sender, RoutedEventArgs e)
        {
            //_scrollViewer = xGridView.GetFirstDescendantOfType<ScrollViewer>();
            //_scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
            //UpdateVisibleIndices(true);
        }

        private int _prevOffset;
        //private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs scrollViewerViewChangingEventArgs)
        //{
        //    UpdateVisibleIndices();
        //}

        //private void UpdateVisibleIndices(bool forceUpdate = false)
        //{
        //    var source = ViewModel.DocumentViewModels;
        //    _scrollViewer.UpdateLayout();
        //    var displayableOnRow = (int)(_scrollViewer.ActualWidth / ViewModel.CellSize);
        //    var displayableOnCol = (int)(_scrollViewer.ActualHeight / ViewModel.CellSize) + 1;
        //    var verticalOffset = (int)(_scrollViewer.VerticalOffset / ViewModel.CellSize);
        //    if (_prevOffset == verticalOffset && !forceUpdate) return;
        //    _prevOffset = verticalOffset;
        //    var firstIndex = verticalOffset * displayableOnRow;
        //    for (var i = firstIndex; i < firstIndex + displayableOnRow * displayableOnCol; i++)
        //    {
        //        Debug.WriteLine(i);
        //        source[i].VisibleOnView = true;
        //    }
        //}

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as BaseCollectionViewModel;

            if (vm != null)
            {
                // remove events from current view model if there is a current view model
                if (ViewModel != null)
                {
                    xGridView.DragItemsStarting -= ViewModel.xGridView_OnDragItemsStarting;
                    xGridView.DragItemsCompleted -= ViewModel.xGridView_OnDragItemsCompleted;
                    xGridView.SelectionChanged -= ViewModel.XGridView_SelectionChanged;
                    xGridView.ContainerContentChanging -= ViewModel.ContainerContentChangingPhaseZero;
                }

                ViewModel = vm;
                ViewModel.SetSelected(this, IsSelected);
                xGridView.DragItemsStarting += ViewModel.xGridView_OnDragItemsStarting;
                xGridView.DragItemsCompleted += ViewModel.xGridView_OnDragItemsCompleted;
                xGridView.SelectionChanged += ViewModel.XGridView_SelectionChanged;
                xGridView.ContainerContentChanging += ViewModel.ContainerContentChangingPhaseZero;
            }
        }

        private void CollectionGridView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                xGridView.DragItemsStarting -= ViewModel.xGridView_OnDragItemsStarting;
                xGridView.DragItemsCompleted -= ViewModel.xGridView_OnDragItemsCompleted;
                xGridView.SelectionChanged -= ViewModel.XGridView_SelectionChanged;
                xGridView.ContainerContentChanging -= ViewModel.ContainerContentChangingPhaseZero;
                xGridView.Loaded -= XGridView_OnLoaded;
                //_scrollViewer.ViewChanging -= ScrollViewerOnViewChanging;
            }
            Unloaded -= CollectionGridView_Unloaded;
        }

        #region ItemSelection

        public void ToggleSelectAllItems()
        {
            ViewModel.ToggleSelectAllItems(xGridView);
        }

        #endregion

        #region DragAndDrop


        private void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragEnter(sender, e);
        }

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDrop(sender, e);
        }

        private void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragLeave(sender, e);
        }

        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
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
