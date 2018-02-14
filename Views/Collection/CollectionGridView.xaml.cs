using Dash.Models.DragModels;
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
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Dash.NoteDocuments;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionGridView : SelectionElement, ICollectionView
    {
        private bool _rightPressed;

        public BaseCollectionViewModel ViewModel { get; private set; }
        //private ScrollViewer _scrollViewer;
        public CollectionGridView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            //Unloaded += CollectionGridView_Unloaded;

            PointerWheelChanged += CollectionGridView_PointerWheelChanged;
        }

        private void CollectionGridView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
            {
                var point = e.GetCurrentPoint(this);

                // get the scale amount
                var scaleAmount = point.Properties.MouseWheelDelta > 0 ? 10 : -10;

                ViewModel.CellSize += scaleAmount;
                var style = new Style(typeof(GridViewItem));
                style.Setters.Add(new Setter(WidthProperty, ViewModel.CellSize));
                style.Setters.Add(new Setter(HeightProperty, ViewModel.CellSize));
                xGridView.ItemContainerStyle = style;
                e.Handled = true;
            }
        }

        public CollectionGridView(BaseCollectionViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
        //private void XGridView_OnLoaded(object sender, RoutedEventArgs e)
        //{
        //    //_scrollViewer = xGridView.GetFirstDescendantOfType<ScrollViewer>();
        //    //_scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
        //    //UpdateVisibleIndices(true);
        //}

        //private int _prevOffset;
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
                ViewModel = vm;

                var style = new Style(typeof(GridViewItem));
                style.Setters.Add(new Setter(WidthProperty, ViewModel.CellSize));
                style.Setters.Add(new Setter(HeightProperty, ViewModel.CellSize));
                xGridView.ItemContainerStyle = style;
            }
        }

        //private void CollectionGridView_Unloaded(object sender, RoutedEventArgs e)
        //{
        //    if (ViewModel != null)
        //    {
        //        //xGridView.DragItemsStarting -= ViewModel.xGridView_OnDragItemsStarting;
        //        //xGridView.DragItemsCompleted -= ViewModel.xGridView_OnDragItemsCompleted;
        //        //xGridView.SelectionChanged -= ViewModel.XGridView_SelectionChanged;
        //        xGridView.ContainerContentChanging -= ViewModel.ContainerContentChangingPhaseZero;
        //        //xGridView.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ViewModel.XGridView_PointerPressed));
        //        //xGridView.Loaded -= XGridView_OnLoaded;
        //        //_scrollViewer.ViewChanging -= ScrollViewerOnViewChanging;
        //    }
        //    Unloaded -= CollectionGridView_Unloaded;
        //}

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
        
        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var cv = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DataDocument;
            e.Handled = true;
            OnSelected();
        }

        #endregion


        private void XGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var dvm = e.Items.Cast<DocumentViewModel>().FirstOrDefault();
            if (dvm != null)
            {
                var drag = new DragDocumentModel(dvm.DocumentController, true);
                e.Data.Properties[nameof(DragDocumentModel)] = drag;
            }
        }

        private void XGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (args.DropResult == DataPackageOperation.None)
            {
                return;
            }
            if (args.DropResult == DataPackageOperation.Move)
            {
                var dvm = args.Items.Cast<DocumentViewModel>().FirstOrDefault();
                if (dvm != null)
                {
                    var pc = this.GetFirstAncestorOfType<CollectionView>();
                    var group = pc?.GetDocumentGroup(dvm.DocumentController) ?? dvm.DocumentController;
                    GroupManager.RemoveGroup(pc, group);
                    ViewModel.RemoveDocument(dvm.DocumentController);
                }
            }
        }
    }
}
