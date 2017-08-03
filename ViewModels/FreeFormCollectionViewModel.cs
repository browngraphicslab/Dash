using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class FreeFormCollectionViewModel : ViewModelBase, ICollectionViewModel
    {
        private ObservableCollection<DocumentViewModel> _documentViewModels;

        public FreeFormCollectionViewModel(bool isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
            DocumentViewModels = new ObservableCollection<DocumentViewModel>();
            CellSize = 250;
            CanDragItems = true;
        }

        public bool IsInterfaceBuilder { get; set; }

        public ObservableCollection<DocumentViewModel> DocumentViewModels
        {
            get => _documentViewModels;
            set => SetProperty(ref _documentViewModels, value);
        }

        public double CellSize { get; set; }
        public bool CanDragItems { get; set; }
        public ListViewSelectionMode ItemSelectionMode { get; set; }


        public void AddDocuments(List<DocumentController> documents, Context context)
        {
            foreach (var docController in documents)
                AddDocument(docController, context);
        }

        public void AddDocument(DocumentController document, Context context)
        {
            var docVm = new DocumentViewModel(document, IsInterfaceBuilder);
            DocumentViewModels.Add(docVm);
        }

        public void RemoveDocuments(List<DocumentController> documents)
        {
            foreach (var doc in documents)
                RemoveDocument(doc);
        }

        public void RemoveDocument(DocumentController document)
        {
            var vmToRemove = DocumentViewModels.FirstOrDefault(vm => vm.DocumentController.GetId() == document.GetId());
            if (vmToRemove != null)
                DocumentViewModels.Remove(vmToRemove);
        }
    }

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
            if (ItemsCarrier.Instance.Source.Equals(viewModel))
                e.AcceptedOperation = DataPackageOperation.Move;
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
            carrier.Destination = viewModel;

            if (carrier.Source == carrier.Destination)
                return; // we don't want to drop items on ourself

            //carrier.Translate = CurrentView is CollectionFreeformView
            //    ? e.GetPosition(((CollectionFreeformView)CurrentView).xItemsControl.ItemsPanelRoot)
            //    : new Point();
            viewModel.AddDocuments(carrier.Payload, null);
        }

        #endregion
    }
}