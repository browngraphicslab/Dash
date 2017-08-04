using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public static class CollectionViewModelExtensions
    {
        #region DragAndDrop

        public static void xGridView_OnDragItemsStarting(this ICollectionViewModel viewModel, object sender,
            DragItemsStartingEventArgs e)
        {
            MainPage.Instance.MainDocView.DragOver -= MainPage.Instance.xCanvas_DragOver;
            var carrier = ItemsCarrier.Instance;
            carrier.Source = viewModel;
            carrier.Payload = e.Items.Cast<DocumentViewModel>().Select(dvm => dvm.DocumentController).ToList();
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        public static void xGridView_OnDragItemsCompleted(this ICollectionViewModel viewModel, ListViewBase sender,
            DragItemsCompletedEventArgs args)
        {
            var carrier = ItemsCarrier.Instance;

            if (carrier.Source == carrier.Destination)
                return; // we don't want to drop items on ourself

            if (args.DropResult == DataPackageOperation.Move)
                viewModel.RemoveDocuments(ItemsCarrier.Instance.Payload);

            carrier.Payload.Clear();
            carrier.Source = null;
            carrier.Destination = null;
            carrier.Translate = new Point();
            MainPage.Instance.MainDocView.DragOver += MainPage.Instance.xCanvas_DragOver;
        }

        public static void CollectionViewOnDragOver(this ICollectionViewModel viewModel, object sender, DragEventArgs e)
        {
            e.Handled = true;

            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;

            if (sourceIsRadialMenu)
                e.AcceptedOperation = DataPackageOperation.Move;

            // don't accept drops from other collections on ourself
            if (ItemsCarrier.Instance.Source != null)
            {
                e.AcceptedOperation = ItemsCarrier.Instance.Source.Equals(viewModel)
                    ? DataPackageOperation.None
                    : DataPackageOperation.Move;

                ItemsCarrier.Instance.Destination = viewModel;
            }
        }

        public static void CollectionViewOnDrop(this ICollectionViewModel viewModel, object sender, DragEventArgs e)
        {
            e.Handled = true;

            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;

            if (sourceIsRadialMenu)
            {
                var action =
                    e.DataView.Properties[RadialMenuView.RadialMenuDropKey] as
                        Action<CollectionView, DragEventArgs>;
                action?.Invoke(MainPage.Instance.GetMainCollectionView(), e);

                return;
            }

            var carrier = ItemsCarrier.Instance;

            if (carrier.Source == null) return;
            //carrier.Destination = viewModel;

            if (carrier.Source.Equals(carrier.Destination))
                return; // we don't want to drop items on ourself

            //carrier.Translate = CurrentView is CollectionFreeformView
            //    ? e.GetPosition(((CollectionFreeformView)CurrentView).xItemsControl.ItemsPanelRoot)
            //    : new Point();
            viewModel.AddDocuments(carrier.Payload, null);
        }

        #endregion
    }
}