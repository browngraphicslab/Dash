﻿using Dash.Controllers.Operators;
using DashShared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static Dash.NoteDocuments;

namespace Dash
{
    public abstract class BaseCollectionViewModel : BaseSelectionElementViewModel, ICollectionViewModel
    {
        private bool _canDragItems;
        private double _cellSize;
        private bool _isInterfaceBuilder;
        private ListViewSelectionMode _itemSelectionMode;
        private static SelectionElement _previousDragEntered;

        public virtual KeyController CollectionKey => KeyStore.CollectionKey;
        public KeyController OutputKey
        {
            get; set;
        }

        protected BaseCollectionViewModel(bool isInInterfaceBuilder) : base(isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
            SelectionGroup = new List<DocumentViewModel>();
        }

        public bool IsInterfaceBuilder
        {
            get { return _isInterfaceBuilder; }
            private set { SetProperty(ref _isInterfaceBuilder, value); }
        }

        public void UpdateDocumentsOnSelection(bool isSelected)
        {
            foreach (var doc in DocumentViewModels)
            {
                doc.IsDraggerVisible = isSelected;
            }
        }

        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();
        public ObservableCollection<DocumentViewModel> ThumbDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();

        // used to keep track of groups of the currently selected items in a collection
        public List<DocumentViewModel> SelectionGroup { get; set; }

        public abstract void AddDocuments(List<DocumentController> documents, Context context);
        public abstract void AddDocument(DocumentController document, Context context);
        public abstract void RemoveDocuments(List<DocumentController> documents);
        public abstract void RemoveDocument(DocumentController document);

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


        DateTime _dragStart = DateTime.MinValue;
        /// <summary>
        /// fired by the starting collection when a drag event is initiated
        /// </summary>
        public void xGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(true);       
            e.Data.Properties.Add("DocumentControllerList", e.Items.Cast<DocumentViewModel>().Select(dvmp => dvmp.DocumentController).ToList());
            e.Data.RequestedOperation = DateTime.Now.Subtract(_dragStart).TotalMilliseconds > 1000 ? DataPackageOperation.Move : DataPackageOperation.Copy;
        }
        public void XGridView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _dragStart = DateTime.Now;
        }

        /// <summary>
        /// fired by the starting collection when a drag event is over
        /// </summary>
        public void xGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs e)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(false);
            
            if (e.DropResult == DataPackageOperation.Move)
                RemoveDocuments(e.Items.Select((i)=>(i as DocumentViewModel).DocumentController).ToList());
        }


        private static KeyController addSubDocs(CollectionDBSchemaHeader.HeaderDragData dragData, List<DocumentController> subDocs, DocumentController d, DocumentController docContext, FieldControllerBase fieldData)
        {
            var fkey = dragData.FieldKey;
            if (fieldData == null)
                foreach (var f in d.EnumFields().Where((ff) => !ff.Key.IsUnrenderedKey())) {
                    fieldData = f.Value;
                    fkey = f.Key;
                    break;
                }

            if (fieldData is TextFieldModelController)
            {
                var newlayout = new TextingBox(new DocumentReferenceFieldController(d.GetDataDocument(null).GetId(), fkey), 0, 0, 200, 100);
                newlayout.Document.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(docContext), true);
                subDocs.Add(newlayout.Document);
            }
            else if(fieldData is NumberFieldModelController)
            {
                var newlayout = new TextingBox(new DocumentReferenceFieldController(d.GetDataDocument(null).GetId(), fkey), 0, 0, 80, 50);
                newlayout.Document.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(docContext), true);
                subDocs.Add(newlayout.Document);
            } else if (fieldData is DocumentFieldModelController)
            {
                subDocs.Add((fieldData as DocumentFieldModelController).Data);
            }
            else
                subDocs.Add(d);
            return fkey;
        }
        /// <summary>
        /// Fired by a collection when an item is dropped on it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            var where = sender is CollectionFreeformView ?
                Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                new Point();
            if (e.DataView != null &&
                  (e.DataView.Properties.ContainsKey(nameof(CollectionDBSchemaHeader.HeaderDragData)) || CollectionDBSchemaHeader.DragModel != null))
            {
                var dragData = e.DataView.Properties.ContainsKey(nameof(CollectionDBSchemaHeader.HeaderDragData)) == true ?
                          e.DataView.Properties[nameof(CollectionDBSchemaHeader.HeaderDragData)] as CollectionDBSchemaHeader.HeaderDragData : CollectionDBSchemaHeader.DragModel;

                // bcz: testing stuff out here...
                var cnote = new CollectionNote(where, dragData.ViewType);
                var getDocs = (dragData.HeaderColumnReference as DocumentReferenceFieldController).DereferenceToRoot(null);
                var subDocs = new List<DocumentController>();
                var showField = dragData.FieldKey;
                foreach (var d in (getDocs as DocumentCollectionFieldModelController).Data)
                {
                    var fieldData = d.GetDataDocument(null).GetDereferencedField(dragData.FieldKey, null);
                    if (fieldData is DocumentFieldModelController)
                        showField = addSubDocs(dragData, subDocs, d, d.GetDataDocument(null), fieldData);
                    else if (fieldData is DocumentCollectionFieldModelController)
                        foreach (var dd in (fieldData as DocumentCollectionFieldModelController).Data)
                            showField = addSubDocs(dragData, subDocs, dd, dd.GetDataDocument(null), null);
                    else
                        showField = addSubDocs(dragData, subDocs, d, d.GetDataDocument(null), fieldData);
                }
                if (subDocs != null)
                    cnote.Document.GetDataDocument(null).SetField(CollectionNote.CollectedDocsKey, new DocumentCollectionFieldModelController(subDocs), true);
                else  cnote.Document.GetDataDocument(null).SetField(CollectionNote.CollectedDocsKey, dragData.HeaderColumnReference, true);
                cnote.Document.GetDataDocument(null).SetField(DBFilterOperatorFieldModelController.FilterFieldKey, new TextFieldModelController(showField.Name), true);

                AddDocument(cnote.Document, null);
                DBTest.DBDoc.AddChild(cnote.Document);
                CollectionDBSchemaHeader.DragModel = null;
                return;
            }

              // first check for things we don't want to allow dropped onto the collection
              //restore previous conditions 
              if (DocumentView.DragDocumentView != null)
                DocumentView.DragDocumentView.IsHitTestVisible = true;
            this.RemoveDragDropIndication(sender as SelectionElement);

            // true if dragged from key value pane in interfacebuilder
            var isDraggedFromKeyValuePane = e.DataView.Properties[KeyValuePane.DragPropertyKey] != null;

            // true if dragged from layoutbar in interfacebuilder
            var isDraggedFromLayoutBar = e.DataView.Properties[InterfaceBuilder.LayoutDragKey]?.GetType() == typeof(InterfaceBuilder.DisplayTypeEnum);
            if (isDraggedFromLayoutBar || isDraggedFromKeyValuePane) return; // in both these cases we don't want the collection to intercept the event

            //return if it's an operator dragged from compoundoperatoreditor listview 
            if (e.Data?.Properties[CompoundOperatorFieldController.OperationBarDragKey] != null) return;

            // from now on we are handling this event!
            e.Handled = true;

            // if we are dragging and dropping from the radial menu
            // if we drag from radial menu
            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;
            if (sourceIsRadialMenu)
            {
                var action =
                    e.DataView.Properties[RadialMenuView.RadialMenuDropKey] as
                        Action<ICollectionView, DragEventArgs>;
                action?.Invoke(sender as ICollectionView, e);
            }
            // if we drag from the file system
            var sourceIsFileSystem = e.DataView.Contains(StandardDataFormats.StorageItems);
            if (sourceIsFileSystem)
            {
                try
                {
                    await FileDropHelper.HandleDropOnCollectionAsync(sender, e, this);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            if (e.DataView != null && e.DataView.Properties.ContainsKey("DocumentControllerList"))
            {
                var collectionViewModel = e.DataView.Properties.ContainsKey(nameof(BaseCollectionViewModel)) == true ?
                          e.DataView.Properties[nameof(BaseCollectionViewModel)] as BaseCollectionViewModel : null;

                var items = e.DataView.Properties.ContainsKey("DocumentControllerList") == true ?
                          e.DataView.Properties["DocumentControllerList"] as List<DocumentController> : null;

                var width = e.DataView.Properties.ContainsKey("Width") == true ? (double)e.DataView.Properties["Width"] : double.NaN;
                var height = e.DataView.Properties.ContainsKey("Height") == true ? (double)e.DataView.Properties["Height"] : double.NaN;

                var payloadLayoutDelegates = items.Select((p) =>
                {
                    if (p.GetActiveLayout() == null && p.GetDereferencedField(KeyStore.DocumentContextKey, null) == null)
                        p.SetActiveLayout(new DefaultLayout().Document, true, true);
                    var newDoc = e.DataView.Properties.ContainsKey("View") ? p.GetViewCopy(where) :
                                                                     e.AcceptedOperation == DataPackageOperation.Move ? p.GetSameCopy(where) :
                                                                     e.AcceptedOperation == DataPackageOperation.Link ? p.GetDataInstance(where) : p.GetCopy(where);
                    if (double.IsNaN(newDoc.GetWidthField().Data))
                        newDoc.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(width), true);
                    if (double.IsNaN(newDoc.GetHeightField().Data))
                        newDoc.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(height), true);
                    return newDoc;
                });
                AddDocuments(payloadLayoutDelegates.ToList(), null);
                if (collectionViewModel == this && e.AcceptedOperation == DataPackageOperation.Move)
                {
                    e.AcceptedOperation = DataPackageOperation.Link; // if the item stayed in the same container, treat it as link, not a move (a move will remove the source object in DragCompleted)
                }
            }

            // return global hit test visibility to be false, 
            SetGlobalHitTestVisiblityOnSelectedItems(false);
        }

        /// <summary>
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            Debug.WriteLine("CollectionViewOnDragEnter Base");
            this.HighlightPotentialDropTarget(sender as SelectionElement);

            SetGlobalHitTestVisiblityOnSelectedItems(true);

            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;
            if (sourceIsRadialMenu)
            {
               e.DragUIOverride.Clear();
                e.DragUIOverride.Caption = e.DataView.Properties.Title;
                e.DragUIOverride.IsContentVisible = false;
                e.DragUIOverride.IsGlyphVisible = false;
            }
            
            e.AcceptedOperation |= (DataPackageOperation.Copy | DataPackageOperation.Move | DataPackageOperation.Link) & (e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation);
            e.DragUIOverride.IsContentVisible = true;

            e.Handled = true;
        }

        

        /// <summary>
        /// Fired by a collection when the item being dragged is no longer over it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            Debug.WriteLine("CollectionViewOnDragLeave Base");
            // fix the problem of CollectionViewOnDragEnter not firing when leaving a collection to the outside one 
            var parentCollection = (sender as DependencyObject).GetFirstAncestorOfType<CollectionView>();
            parentCollection?.ViewModel?.CollectionViewOnDragEnter(parentCollection.CurrentView, e);

            var element = sender as SelectionElement;
            if (element != null)
            {
                var color = ((SolidColorBrush)App.Instance.Resources["DragHighlight"]).Color;
                this.ChangeIndicationColor(element, color);
                element.HasDragLeft = true;
                var parent = element.ParentSelectionElement;
                // if the current collection fires a dragleave event and its parent hasn't
                if (parent != null && !parent.HasDragLeft)
                {
                    this.ChangeIndicationColor(parent, color);
                }
            }
            this.RemoveDragDropIndication(sender as SelectionElement);
            e.Handled = true;
        }

        /// <summary>
        /// Highlight a collection when drag enters it to indicate which collection would the document move to if the user were to drop it now
        /// </summary>
        private void HighlightPotentialDropTarget(SelectionElement element)
        {
            // change background of collection to indicate which collection is the potential drop target, determined by the drag entered event
            if (element != null)
            {
                // only one collection should be highlighted at a time
                if (_previousDragEntered != null)
                {
                    this.ChangeIndicationColor(_previousDragEntered, Colors.Transparent);
                }
                var color = ((SolidColorBrush)App.Instance.Resources["DragHighlight"]).Color;
                element.HasDragLeft = false;
                _previousDragEntered = element;
                this.ChangeIndicationColor(element, color);
            }
        }

        /// <summary>
        /// Remove highlight from target drop collection and border from DocumentView being dragged
        /// </summary>
        /// <param name="element"></param>
        private void RemoveDragDropIndication(SelectionElement element)
        {
            // remove drop target indication when doc is dropped
            if (element != null)
            {
                this.ChangeIndicationColor(element, Colors.Transparent);
                _previousDragEntered = null;
            }

            // remove border from DocumentView once it is dropped onto a collection
            if (DocumentView.DragDocumentView != null)
                DocumentView.DragDocumentView.OuterGrid.BorderThickness = new Thickness(0);
            DocumentView.DragDocumentView = null;
        }

        public void ChangeIndicationColor(SelectionElement element, Color fill)
        {
            (element as CollectionFreeformView)?.SetDropIndicationFill(new SolidColorBrush(fill));
            (element as CollectionGridView)?.SetDropIndicationFill(new SolidColorBrush(fill));
            (element as CollectionListView)?.SetDropIndicationFill(new SolidColorBrush(fill));
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
            SelectionGroup.AddRange(listViewBase?.SelectedItems.Cast<DocumentViewModel>());
        }

        #endregion

        #region Virtualization

        public void ContainerContentChangingPhaseZero(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            //args.Handled = true;
            //if (args.Phase != 0) throw new Exception("Please start in stage 0");
            //var rootGrid = (Grid)args.ItemContainer.ContentTemplateRoot;
            //var backdrop = (DocumentView)rootGrid?.FindName("XBackdrop");
            //var border = (Viewbox)rootGrid?.FindName("xBorder");
            //Debug.Assert(backdrop != null, "backdrop != null");
            //backdrop.Visibility = Visibility.Visible;
            //backdrop.ClearValue(FrameworkElement.WidthProperty);
            //backdrop.ClearValue(FrameworkElement.HeightProperty);
            //backdrop.Width = backdrop.Height = 250;
            //backdrop.xProgressRing.Visibility = Visibility.Visible;
            //backdrop.xProgressRing.IsActive = true;
            //Debug.Assert(border != null, "border != null");
            //border.Visibility = Visibility.Collapsed;
            //args.RegisterUpdateCallback(ContainerContentChangingPhaseOne);

            //if (args.Phase != 0)
            //{
            //    throw new Exception("We should be in phase 0 but we are not");
            //}

            //args.RegisterUpdateCallback(ContainerContentChangingPhaseOne);
            //args.Handled = true;
        }

        private void ContainerContentChangingPhaseOne(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            //if (args.Phase != 1) throw new Exception("Please start in phase 1");
            //var rootGrid = (Grid)args.ItemContainer.ContentTemplateRoot;
            //var backdrop = (DocumentView)rootGrid?.FindName("XBackdrop");
            //var border = (Viewbox)rootGrid?.FindName("xBorder");
            //var document = (DocumentView)border?.FindName("xDocumentDisplay");
            //Debug.Assert(backdrop != null, "backdrop != null");
            //Debug.Assert(border != null, "border != null");
            //Debug.Assert(document != null, "document != null");
            //backdrop.Visibility = Visibility.Collapsed;
            //backdrop.xProgressRing.IsActive = false;
            //border.Visibility = Visibility.Visible;
            //document.IsHitTestVisible = false;
            //var dvParams = ((ObservableCollection<DocumentViewModelParameters>)sender.ItemsSource)?[args.ItemIndex];

            //if (document.ViewModel == null)
            //{
            //    document.DataContext =
            //        new DocumentViewModel(dvParams.Controller, dvParams.IsInInterfaceBuilder, dvParams.Context);               
            //}
            //else if (document.ViewModel.DocumentController.GetId() != dvParams.Controller.GetId())
            //{
            //    document.ViewModel.Dispose();
            //    document.DataContext =
            //        new DocumentViewModel(dvParams.Controller, dvParams.IsInInterfaceBuilder, dvParams.Context);
            //}
            //else
            //{
            //    document.ViewModel.Dispose();
            //}
        }

        #endregion

    }
}