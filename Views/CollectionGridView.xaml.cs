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
        public GridView XGridView => xGridView;

        public CollectionGridView(BaseCollectionViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            if (ViewModel == null) return;
            xGridView.DragItemsStarting += ViewModel.xGridView_OnDragItemsStarting;
            xGridView.DragItemsCompleted += ViewModel.xGridView_OnDragItemsCompleted;
            xGridView.SelectionChanged += ViewModel.XGridView_SelectionChanged;
            this.Unloaded += CollectionGridView_Unloaded;
        }

        private void CollectionGridView_Unloaded(object sender, RoutedEventArgs e)
        {
            xGridView.DragItemsStarting -= ViewModel.xGridView_OnDragItemsStarting;
            xGridView.DragItemsCompleted -= ViewModel.xGridView_OnDragItemsCompleted;
            xGridView.SelectionChanged -= ViewModel.XGridView_SelectionChanged;
            this.Unloaded -= CollectionGridView_Unloaded;
        }

        #region ItemSelection

        public void ToggleSelectAllItems()
        {
            ViewModel.ToggleSelectAllItems(xGridView);
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

        private void XGridView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            args.Handled = true;
            if (args.Phase != 0) throw new Exception("Please start in stage 0");
            var rootGrid = (Grid)args.ItemContainer.ContentTemplateRoot;
            var backdrop = (DocumentView)rootGrid?.FindName("XBackdrop");
            var border = (Viewbox)rootGrid?.FindName("xBorder");
            Debug.Assert(backdrop != null, "backdrop != null");
            backdrop.Visibility = Visibility.Visible;
            backdrop.ClearValue(WidthProperty);
            backdrop.ClearValue(HeightProperty);
            backdrop.Width = backdrop.Height = 250;
            backdrop.xProgressRing.Visibility = Visibility.Visible;
            backdrop.xProgressRing.IsActive = true;
            Debug.Assert(border != null, "border != null");
            border.Visibility = Visibility.Collapsed;
            args.RegisterUpdateCallback(RenderDocumentPhaseOne);
        }
        private void RenderDocumentPhaseOne(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 1) throw new Exception("Please start in phase 1");
            var rootGrid = (Grid)args.ItemContainer.ContentTemplateRoot;
            var backdrop = (DocumentView)rootGrid.FindName("XBackdrop");
            var border = (Viewbox)rootGrid.FindName("xBorder");
            var document = (DocumentView)border.FindName("xDocumentDisplay");
            backdrop.Visibility = Visibility.Collapsed;
            backdrop.xProgressRing.IsActive = false;
            border.Visibility = Visibility.Visible;
            document.IsHitTestVisible = false;
            var dvParams = ((ObservableCollection<DocumentViewModelParameters>)xGridView.ItemsSource)?[args.ItemIndex];
            Debug.Assert(dvParams != null, "dvParams != null");
            var vm = new DocumentViewModel(dvParams.Controller, dvParams.IsInInterfaceBuilder, dvParams.Context);
            document.DataContext = vm;
        }
    }
}
