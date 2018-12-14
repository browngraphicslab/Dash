using System;
using System.Collections.Generic;
using System.Linq;
using Dash.FontIcons;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionGridView : UserControl, ICollectionView
    {
        private int _cellSize = 250;
        public UserControl         UserControl => this;
        public CollectionViewModel ViewModel   => DataContext as CollectionViewModel;
        public CollectionViewType ViewType => CollectionViewType.Grid;
        //private ScrollViewer _scrollViewer;
        public CollectionGridView()
        {
            this.InitializeComponent();
            PointerWheelChanged += CollectionGridView_PointerWheelChanged;

            Loaded += CollectionGridView_Loaded;
        }

        public void SetupContextMenu(MenuFlyout contextMenu)
        {
            contextMenu.Items.Add(new MenuFlyoutSubItem()
            {
                Text = "View Children As",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Eye }
            });
            foreach (var n in Enum.GetValues(typeof(CollectionViewType)).Cast<CollectionViewType>())
            {
                (contextMenu.Items.Last() as MenuFlyoutSubItem).Items.Add(new MenuFlyoutItem() { Text = n.ToString() });
                ((contextMenu.Items.Last() as MenuFlyoutSubItem).Items.Last() as MenuFlyoutItem).Click += (ss, ee) => {
                    foreach (var dvm in ViewModel.DocumentViewModels)
                    {
                        dvm.LayoutDocument.SetField<TextController>(KeyStore.CollectionViewTypeKey, n.ToString(), true);
                    }
                };
            }
        }

        private void CollectionGridView_Loaded(object sender, RoutedEventArgs e)
        {
            var selectedDocControllers =
                SelectionManager.GetSelectedDocs().Select(dv => dv.ViewModel?.DocumentController).ToList();
            foreach (var i in xGridView.Items.OfType<DocumentViewModel>())
            {
                var d = i.DocumentController;
                if (selectedDocControllers.Contains(d))
                    xGridView.SelectedItem = i;
            }
            xGridView.SelectionChanged += XGridView_SelectionChanged;
        }
        public void OnDocumentSelected(bool selected)
        {
        }

        private void XGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                //SelectionManager.Select(this.GetDescendantsOfType<DocumentView>().FirstOrDefault(dv => dv.ViewModel.DocumentController.Equals((e.AddedItems.First() as DocumentViewModel).DocumentController)), false);
            }
        }

        private void CollectionGridView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (this.IsCtrlPressed() && false)
            {
                var point = e.GetCurrentPoint(this);

                // get the scale amount
                var scaleAmount = point.Properties.MouseWheelDelta > 0 ? 10 : -10;
                if (_cellSize + scaleAmount > 10 && _cellSize + scaleAmount < 1000)
                {
                    
                    _cellSize += scaleAmount;

                    ((ItemsWrapGrid)xGridView.ItemsPanelRoot).ItemWidth = _cellSize;
                    ((ItemsWrapGrid)xGridView.ItemsPanelRoot).ItemHeight = double.NaN;
                    e.Handled = true;
                }
                
            }
        }
        
        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
        }

        private DocumentViewModel _dragDoc;
        private void XGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            foreach (var m in e.Items.OfType<DocumentViewModel>())
            {
                _dragDoc = m;
                e.Data.SetDragModel(new DragDocumentModel(m.DocumentController) { DraggedDocCollectionViews = new List<CollectionViewModel> { ViewModel } });
            }
        }
        private void xGridView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (this.IsPointerOver()) //  && args.DropResult == DataPackageOperation.Move)
            {
                DocumentView found = null;
                foreach (var doc in xGridView.ItemsPanelRoot.Children.Select((gvi)=> gvi.GetFirstDescendantOfType<DocumentView>()))
                {
                    if (doc.GetBoundingRect(doc).Contains(doc.PointerPos()))
                    {
                        found = doc;
                    }
                }
                if (found != null)
                {
                    var ind = ViewModel.DocumentViewModels.IndexOf(found.ViewModel);
                    ViewModel.RemoveDocument(_dragDoc.DocumentController);
                    ViewModel.InsertDocument(_dragDoc.DocumentController, ind);
                }
            }

        }

        private void xDocumentWrapper_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var dv = (sender as Grid).Children.FirstOrDefault() as DocumentView;
            dv.TappedHandler(false);
            e.Handled = true;
        }
    }
}
