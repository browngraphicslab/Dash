using System.Linq;
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
        int _cellSize = 250;
        public UserControl         UserControl => this;
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }
        //private ScrollViewer _scrollViewer;
        public CollectionGridView()
        {
            this.InitializeComponent();
            PointerWheelChanged += CollectionGridView_PointerWheelChanged;

            Loaded += CollectionGridView_Loaded;
        }

        private void CollectionGridView_Loaded(object sender, RoutedEventArgs e)
        {
            var selectedDocControllers =
                SelectionManager.GetSelectedDocs().Select(dv => dv.ViewModel.DocumentController).ToList();
            foreach (var i in xGridView.Items.OfType<DocumentViewModel>())
            {
                var d = i.DocumentController;
                if (selectedDocControllers.Contains(d))
                    xGridView.SelectedItem = i;
            }
            xGridView.SelectionChanged += XGridView_SelectionChanged;
        }

        private void XGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                SelectionManager.Select(this.GetDescendantsOfType<DocumentView>().FirstOrDefault(dv => dv.ViewModel.DocumentController.Equals((e.AddedItems.First() as DocumentViewModel).DocumentController)), false);
            }
        }

        private void CollectionGridView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (this.IsCtrlPressed())
            {
                var point = e.GetCurrentPoint(this);

                // get the scale amount
                var scaleAmount = point.Properties.MouseWheelDelta > 0 ? 10 : -10;
                if (_cellSize + scaleAmount > 10 && _cellSize + scaleAmount < 1000)
                {
                    
                    _cellSize += scaleAmount;

                    ((ItemsWrapGrid)xGridView.ItemsPanelRoot).ItemWidth = _cellSize;
                    ((ItemsWrapGrid)xGridView.ItemsPanelRoot).ItemHeight = _cellSize;
                    e.Handled = true;
                }
                
            }
        }

        #region DragAndDrop
        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
        }
        #endregion
        

        private void XGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            DocumentViewModel dvm = e.Items.Cast<DocumentViewModel>().FirstOrDefault();
            if (dvm == null) return;

            e.Data.AddDragModel(new DragDocumentModel(dvm.DocumentController));
        }

        private void XGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (args.DropResult == DataPackageOperation.Move)
            {
                var dvm = args.Items.Cast<DocumentViewModel>().FirstOrDefault();
                if (dvm != null)
                {
                    ViewModel.RemoveDocument(dvm.DocumentController);
                }
            }
        }

        private void Viewbox_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var dv = ((sender as Border).Child as Viewbox).Child as DocumentView;
            MainPage.Instance.NavigateToDocumentInWorkspace(dv.ViewModel.DocumentController, true, true, true);
        }
    }
}
