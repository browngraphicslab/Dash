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
    public abstract class BaseCollectionViewModel : BaseSelectionElementViewModel, ICollectionViewModel
    {
        private bool _canDragItems;
        private double _cellSize;
        private ObservableCollection<DocumentViewModel> _documentViewModels;
        private bool _isInterfaceBuilder;
        private ListViewSelectionMode _itemSelectionMode;

        protected BaseCollectionViewModel(bool isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
            _documentViewModels = new ObservableCollection<DocumentViewModel>();
            SelectionGroup = new List<DocumentViewModel>();
        }

        public bool IsInterfaceBuilder
        {
            get => _isInterfaceBuilder;
            private set => SetProperty(ref _isInterfaceBuilder, value);
        }

        public ObservableCollection<DocumentViewModel> DocumentViewModels
        {
            get => _documentViewModels;
            protected set => SetProperty(ref _documentViewModels, value);
        }

        // used to keep track of groups of the currently selected items in a collection
        public List<DocumentViewModel> SelectionGroup { get; }

        public abstract void AddDocuments(List<DocumentController> documents, Context context);
        public abstract void AddDocument(DocumentController document, Context context);
        public abstract void RemoveDocuments(List<DocumentController> documents);
        public abstract void RemoveDocument(DocumentController document);


        #region Grid or List Specific Variables I want to Remove

        public double CellSize
        {
            get => _cellSize;
            protected set => SetProperty(ref _cellSize, value);
        }

        public bool CanDragItems
        {
            get => _canDragItems;
            set => SetProperty(ref _canDragItems, value);
// 
        }

        public ListViewSelectionMode ItemSelectionMode
        {
            get => _itemSelectionMode;
            set => SetProperty(ref _itemSelectionMode, value);
        }

        #endregion


        #region DragAndDrop

        public void xGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(true);

            MainPage.Instance.MainDocView.DragOver -= MainPage.Instance.xCanvas_DragOver;
            var carrier = ItemsCarrier.Instance;
            carrier.Source = this;
            carrier.Payload = e.Items.Cast<DocumentViewModel>().Select(dvm => dvm.DocumentController).ToList();
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        public void xGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(false);

            var carrier = ItemsCarrier.Instance;

            if (carrier.Source == carrier.Destination)
                return; // we don't want to drop items on ourself

            if (args.DropResult == DataPackageOperation.Move)
                RemoveDocuments(ItemsCarrier.Instance.Payload);

            carrier.Payload.Clear();
            carrier.Source = null;
            carrier.Destination = null;
            carrier.Translate = new Point();
            MainPage.Instance.MainDocView.DragOver += MainPage.Instance.xCanvas_DragOver;
        }

        public void CollectionViewOnDragOver(object sender, DragEventArgs e)
        {
            var isDraggedFromKeyValuePane = e.DataView.Properties[KeyValuePane.DragPropertyKey] != null;
            var isDraggedFromLayoutBar = e.DataView.Properties[InterfaceBuilder.LayoutDragKey]?.GetType() == typeof(InterfaceBuilder.DisplayTypeEnum);
            if (isDraggedFromLayoutBar || isDraggedFromKeyValuePane) return;
            e.Handled = true;

            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;

            if (sourceIsRadialMenu)
                e.AcceptedOperation = DataPackageOperation.Move;

            // don't accept drops from other collections on ourself
            if (ItemsCarrier.Instance.Source != null)
            {
                e.AcceptedOperation = ItemsCarrier.Instance.Source.Equals(this)
                    ? DataPackageOperation.None
                    : DataPackageOperation.Move;

                ItemsCarrier.Instance.Destination = this;
            }
        }

        public void CollectionViewOnDrop(object sender, DragEventArgs e)
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
            AddDocuments(carrier.Payload, null);
        }

        public void ToggleSelectAllItems(ListViewBase listView)
        {
            var isAllItemsSelected = listView.SelectedItems.Count == DocumentViewModels.Count;
            if (!isAllItemsSelected)
                listView.SelectAll();
            else
                listView.SelectedItems.Clear();
        }

        public void XGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listViewBase = sender as ListViewBase;
            SelectionGroup.Clear();
            SelectionGroup.AddRange(listViewBase.SelectedItems.Cast<DocumentViewModel>());
        }

        #endregion
    }
}