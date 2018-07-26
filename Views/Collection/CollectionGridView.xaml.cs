using Dash.Models.DragModels;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionGridView : UserControl, ICollectionView
    {
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }
        //private ScrollViewer _scrollViewer;
        public CollectionGridView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            AddHandler(PointerPressedEvent, new PointerEventHandler(CollectionGridView_PointerPressed), true);


            PointerWheelChanged += CollectionGridView_PointerWheelChanged;

            Loaded += CollectionGridView_Loaded;
        }

        private void CollectionGridView_Loaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.RefreshSelected(this.GetDescendantsOfType<DocumentView>());
            var selected = SelectionManager.SelectedDocs.Select((dv) => dv.ViewModel.DocumentController).ToList();
            foreach (var i in xGridView.Items.OfType<DocumentViewModel>())
            {
                var d = i.DocumentController;
                if (selected.Contains(d))
                    xGridView.SelectedItem = i;
            }
            xGridView.SelectionChanged += XGridView_SelectionChanged;
        }

        private void XGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                SelectionManager.DeselectAll();
                SelectionManager.Select(this.GetDescendantsOfType<DocumentView>().Where((dv) => dv.ViewModel.DocumentController.Equals((e.AddedItems.First() as DocumentViewModel).DocumentController)).FirstOrDefault());

            }

        }

        private void CollectionGridView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var docview = this.GetFirstAncestorOfType<DocumentView>();
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                docview.ManipulationMode = ManipulationModes.All;
            }
            else
            {
                docview.ManipulationMode = ManipulationModes.None;
                //SelectionManager.Select(docview);
                
            }

            e.Handled = true;


        }

        private void CollectionGridView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (this.IsCtrlPressed())
            {
                var point = e.GetCurrentPoint(this);

                // get the scale amount
                var scaleAmount = point.Properties.MouseWheelDelta > 0 ? 10 : -10;
                if (ViewModel.CellSize + scaleAmount > 10 && ViewModel.CellSize + scaleAmount < 1000)
                {
                    ViewModel.CellSize += scaleAmount;
                    var style = new Style(typeof(GridViewItem));
                    style.Setters.Add(new Setter(WidthProperty, ViewModel.CellSize));
                    style.Setters.Add(new Setter(HeightProperty, ViewModel.CellSize));
                    xGridView.ItemContainerStyle = style;
                }
                e.Handled = true;
            }
        }

        public CollectionGridView(CollectionViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var style = new Style(typeof(GridViewItem));
            style.Setters.Add(new Setter(WidthProperty, ViewModel.CellSize));
            style.Setters.Add(new Setter(HeightProperty, ViewModel.CellSize));
            xGridView.ItemContainerStyle = style;
        }

        #region DragAndDrop
        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
        }
        #endregion

        #region Activation

        //private void OnTapped(object sender, TappedRoutedEventArgs e)
        //{
        //    var cv = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DataDocument;
        //    e.Handled = true;
        //}

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
