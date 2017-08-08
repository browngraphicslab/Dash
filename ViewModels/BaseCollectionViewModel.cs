using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private ObservableCollection<DocumentViewModelParameters> _documentViewModels;
        private bool _isInterfaceBuilder;
        private ListViewSelectionMode _itemSelectionMode;

        protected BaseCollectionViewModel(bool isInInterfaceBuilder) : base(isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
            _documentViewModels = new ObservableCollection<DocumentViewModelParameters>();
            SelectionGroup = new List<DocumentViewModelParameters>();
        }

        public bool IsInterfaceBuilder
        {
            get { return _isInterfaceBuilder; }
            private set { SetProperty(ref _isInterfaceBuilder, value); } 
        }

        public ObservableCollection<DocumentViewModelParameters> DocumentViewModels
        {
            get { return _documentViewModels; }
            protected set { SetProperty(ref _documentViewModels, value); } 
        }

        // used to keep track of groups of the currently selected items in a collection
        public List<DocumentViewModelParameters> SelectionGroup { get; }

        public abstract void AddDocuments(List<DocumentController> documents, Context context);
        public abstract void AddDocument(DocumentController document, Context context);
        public abstract void RemoveDocuments(List<DocumentController> documents);
        public abstract void RemoveDocument(DocumentController document);


        private void DisplayDocument(ICollectionView collectionView, DocumentController docController, Point? where = null)
        {
            if (where != null)
            {
                var h = docController.GetHeightField().Data;
                var w = docController.GetWidthField().Data;

                w = double.IsNaN(w) ? 0 : w;
                h = double.IsNaN(h) ? 0 : h;

                var pos = (Point)where;
                docController.GetPositionField().Data = new Point(pos.X - w / 2, pos.Y - h / 2);
            }
            collectionView.ViewModel.AddDocument(docController, null);
            DBTest.DBDoc.AddChild(docController);
        }

        private void DisplayDocuments(ICollectionView collectionView, IEnumerable<DocumentController> docControllers, Point? where = null)
        {
            foreach (var documentController in docControllers)
            {
                DisplayDocument(collectionView, documentController, where);
            }
        }

        #region Grid or List Specific Variables I want to Remove

        public double CellSize
        {
            get { return _cellSize; }
            protected set { SetProperty(ref _cellSize, value); } 
        }

        public bool CanDragItems
        {
            get { return _canDragItems; } 
            set { SetProperty(ref _canDragItems, value); } 
// 
        }

        public ListViewSelectionMode ItemSelectionMode
        {
            get { return _itemSelectionMode; } 
            set { SetProperty(ref _itemSelectionMode, value); } 
        }

        #endregion


        #region DragAndDrop

        /// <summary>
        /// fired by the starting collection when a drag event is initiated
        /// </summary>
        public void xGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(true);

            var carrier = ItemsCarrier.Instance;
            carrier.Source = this;
            carrier.Payload = e.Items.Cast<DocumentViewModelParameters>().Select(dvmp => dvmp.Controller).ToList();
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        /// <summary>
        /// fired by the starting collection when a drag event is over
        /// </summary>
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
        }

        /// <summary>
        /// Fired by a collection when an item is dropped on it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;
            if (sourceIsRadialMenu)
            {
                var action =
                    e.DataView.Properties[RadialMenuView.RadialMenuDropKey] as
                        Action<ICollectionView, DragEventArgs>;
                action?.Invoke(sender as ICollectionView, e);
            }

            var carrier = ItemsCarrier.Instance;
            var sourceIsCollection = carrier.Source != null;
            if (sourceIsCollection)
            {

                if (carrier.Source.Equals(carrier.Destination))
                    return; // we don't want to drop items on ourself

                var where = sender is CollectionFreeformView ?
                    Util.GetCollectionDropPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                    new Point();

                DisplayDocuments(sender as ICollectionView, carrier.Payload, where);
            }

            SetGlobalHitTestVisiblityOnSelectedItems(false);
        }

        /// <summary>
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(true);

            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;

            if (sourceIsRadialMenu)
                e.AcceptedOperation = DataPackageOperation.Move;

            var sourceIsCollection = ItemsCarrier.Instance.Source != null;
            if (sourceIsCollection)
            {
                var sourceIsOurself = ItemsCarrier.Instance.Source.Equals(this);
                e.AcceptedOperation = sourceIsOurself
                    ? DataPackageOperation.None // don't accept drag event from ourself
                    : DataPackageOperation.Move;

                ItemsCarrier.Instance.Destination = this;
            }

            // the soruce is assumed to be outside the app
            if ((e.AllowedOperations & DataPackageOperation.Move) != 0)
            {
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        #endregion


        #region Selection

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
            SelectionGroup.AddRange(listViewBase?.SelectedItems.Cast<DocumentViewModelParameters>());
        }

        #endregion


    }
}