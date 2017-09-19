using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public sealed partial class CollectionTextView : SelectionElement, ICollectionView
    {
        public BaseCollectionViewModel ViewModel { get; private set; }


        public CollectionTextView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += CollectionListView_Unloaded;
        }


        public CollectionTextView(BaseCollectionViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as BaseCollectionViewModel;

            if (vm != null)
            {
                // remove events from current view model if there is a current view model
                if (ViewModel != null)
                {
                    xListView.DragItemsStarting -= ViewModel.xGridView_OnDragItemsStarting;
                    xListView.DragItemsCompleted -= ViewModel.xGridView_OnDragItemsCompleted;
                    xListView.SelectionChanged -= ViewModel.XGridView_SelectionChanged;
                    xListView.ContainerContentChanging -= ViewModel.ContainerContentChangingPhaseZero;
                    xListView.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ViewModel.XGridView_PointerPressed));
                }

                ViewModel = vm;
                ViewModel.SetSelected(this, IsSelected);
                xListView.DragItemsStarting += ViewModel.xGridView_OnDragItemsStarting;
                xListView.DragItemsCompleted += ViewModel.xGridView_OnDragItemsCompleted;
                xListView.SelectionChanged += ViewModel.XGridView_SelectionChanged;
                xListView.ContainerContentChanging += ViewModel.ContainerContentChangingPhaseZero;
                xListView.PointerPressed += ViewModel.XGridView_PointerPressed;
                xListView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ViewModel.XGridView_PointerPressed), true);
            }
        }

        private void CollectionListView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                xListView.DragItemsStarting -= ViewModel.xGridView_OnDragItemsStarting;
                xListView.DragItemsCompleted -= ViewModel.xGridView_OnDragItemsCompleted;
                xListView.SelectionChanged -= ViewModel.XGridView_SelectionChanged;
                xListView.ContainerContentChanging -= ViewModel.ContainerContentChangingPhaseZero;
                xListView.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ViewModel.XGridView_PointerPressed));
            }
            this.Unloaded -= CollectionListView_Unloaded;
        }

        #region ItemSelection
        public void ToggleSelectAllItems()
        {
            ViewModel.ToggleSelectAllItems(xListView);
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
