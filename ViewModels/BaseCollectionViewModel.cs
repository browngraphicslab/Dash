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

        public virtual KeyController CollectionKey => DocumentCollectionFieldModelController.CollectionKey;

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
        public List<DocumentViewModelParameters> SelectionGroup { get; set; }

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
            //DBTest.DBDoc.AddChild(docController);
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

            var isDraggedFromKeyValuePane = e.DataView.Properties[KeyValuePane.DragPropertyKey] != null;
            var isDraggedFromLayoutBar = e.DataView.Properties[InterfaceBuilder.LayoutDragKey]?.GetType() == typeof(InterfaceBuilder.DisplayTypeEnum);
            if (isDraggedFromLayoutBar || isDraggedFromKeyValuePane) return;

            // handle but only if it's not in a compoundoperatoreditor view 
            if ((sender as CollectionFreeformView)?.GetFirstAncestorOfType<CompoundOperatorEditor>() == null)
                e.Handled = true;
            else
                return;

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
                carrier.Destination = this; 
                if (carrier.Source.Equals(carrier.Destination))
                {
                    return; // we don't want to drop items on ourself
                }

                var where = sender is CollectionFreeformView ?
                    Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                    new Point();

                DisplayDocuments(sender as ICollectionView, carrier.Payload, where);
                carrier.Payload.Clear();
                carrier.Source = null;
                carrier.Destination = null;
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
            {
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.Clear();
                e.DragUIOverride.Caption = e.DataView.Properties.Title;
                e.DragUIOverride.IsContentVisible = false;
                e.DragUIOverride.IsGlyphVisible = false;
                
            }
                

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

        public void ToggleSelectFreeformView()
        {

        }

        public void XGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listViewBase = sender as ListViewBase;
            SelectionGroup.Clear();
            SelectionGroup.AddRange(listViewBase?.SelectedItems.Cast<DocumentViewModelParameters>());
        }

        #endregion

        #region Virtualization

        public void ContainerContentChangingPhaseZero(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            args.Handled = true;
            if (args.Phase != 0) throw new Exception("Please start in stage 0");
            var rootGrid = (Grid)args.ItemContainer.ContentTemplateRoot;
            var backdrop = (DocumentView)rootGrid?.FindName("XBackdrop");
            var border = (Viewbox)rootGrid?.FindName("xBorder");
            Debug.Assert(backdrop != null, "backdrop != null");
            backdrop.Visibility = Visibility.Visible;
            backdrop.ClearValue(FrameworkElement.WidthProperty);
            backdrop.ClearValue(FrameworkElement.HeightProperty);
            backdrop.Width = backdrop.Height = 250;
            backdrop.xProgressRing.Visibility = Visibility.Visible;
            backdrop.xProgressRing.IsActive = true;
            Debug.Assert(border != null, "border != null");
            border.Visibility = Visibility.Collapsed;
            args.RegisterUpdateCallback(ContainerContentChangingPhaseOne);
        }

        private void ContainerContentChangingPhaseOne(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 1) throw new Exception("Please start in phase 1");
            var rootGrid = (Grid)args.ItemContainer.ContentTemplateRoot;
            var backdrop = (DocumentView)rootGrid?.FindName("XBackdrop");
            var border = (Viewbox)rootGrid?.FindName("xBorder");
            var document = (DocumentView)border?.FindName("xDocumentDisplay");
            Debug.Assert(backdrop != null, "backdrop != null");
            Debug.Assert(border != null, "border != null");
            Debug.Assert(document != null, "document != null");
            backdrop.Visibility = Visibility.Collapsed;
            backdrop.xProgressRing.IsActive = false;
            border.Visibility = Visibility.Visible;
            document.IsHitTestVisible = false;
            var dvParams = ((ObservableCollection<DocumentViewModelParameters>)sender.ItemsSource)?[args.ItemIndex];

            if (document.ViewModel == null)
            {
                document.DataContext =
                    new DocumentViewModel(dvParams.Controller, dvParams.IsInInterfaceBuilder, dvParams.Context);               
            }
            else if (document.ViewModel.DocumentController.GetId() != dvParams.Controller.GetId())
            {
                document.ViewModel.Dispose();
                document.DataContext =
                    new DocumentViewModel(dvParams.Controller, dvParams.IsInInterfaceBuilder, dvParams.Context);
            }
            else
            {
                document.ViewModel.Dispose();
            }
        }

        #endregion

    }
}