using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation.Collections;
using Dash.Views;
using DashShared;
using DocumentMenu;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : SelectionElement
    {

        public int MaxZ { get; set; }


        // whether the user can draw links currently or not
        public bool CanLink
        {
            get
            {
                if (CurrentView is CollectionFreeformView)
                    return (CurrentView as CollectionFreeformView).CanLink;
                return false;
            }
            set
            {
                if (CurrentView is CollectionFreeformView)
                    (CurrentView as CollectionFreeformView).CanLink = value;
            }
        }

        public PointerRoutedEventArgs PointerArgs
        {
            get
            {
                if (CurrentView is CollectionFreeformView)
                    return (CurrentView as CollectionFreeformView).PointerArgs;
                return null;
            }
            set
            {
                if (CurrentView is CollectionFreeformView)
                    (CurrentView as CollectionFreeformView).PointerArgs = value;
            }

        }

        //i think this belong elsewhere
        public static Graph<string> Graph = new Graph<string>();

        public UserControl CurrentView { get; set; }
        private OverlayMenu _colMenu = null;

        public CollectionViewModel ViewModel
        {
            get
            {
                return DataContext as CollectionViewModel;
            }
            set { DataContext = value; }
        }

        public CollectionView ParentCollection { get; set; }
        public DocumentView ParentDocument { get; set; }


        public CollectionView(CollectionViewModel vm) : base()
        {
            this.InitializeComponent();
            ViewModel = vm;
            CurrentView = new CollectionFreeformView();
            xContentControl.Content = CurrentView;
            SetEventHandlers();
            CanLink = true;
        }

        private void SetEventHandlers()
        {
            Loaded += CollectionView_Loaded;
            ViewModel.DataBindingSource.CollectionChanged += DataBindingSource_CollectionChanged;
            DocumentViewContainerGrid.DragOver += CollectionGrid_DragOver;
            DocumentViewContainerGrid.Drop += CollectionGrid_Drop;
            ConnectionEllipse.ManipulationStarted += ConnectionEllipse_OnManipulationStarted;
            Tapped += CollectionView_Tapped;
        }

        private void CollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>();
            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            if (ParentDocument == MainPage.Instance.MainDocView)
            {
                ParentDocument.HasCollection = true;
                //Temporary graphical hax.to be removed when collectionview menu moved to its document.
                ParentDocument.XGrid.Background = new SolidColorBrush(Colors.Transparent);
                ParentDocument.xBorder.Margin = new Thickness(ParentDocument.xBorder.Margin.Left + 5,
                    ParentDocument.xBorder.Margin.Top + 5,
                    ParentDocument.xBorder.Margin.Right,
                    ParentDocument.xBorder.Margin.Bottom);
                OpenMenu();
                ParentSelectionElement?.SetSelectedElement(this);
                xOuterGrid.BorderThickness = new Thickness(0);
            }
        }

        /// <summary>
        /// Update document view model representatino of items on internal collection change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataBindingSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    var docVM = eNewItem as DocumentViewModel;
                    Debug.Assert(docVM != null);
                    var ofm =
                        docVM.DocumentController.GetDereferencedField(OperatorDocumentModel.OperatorKey, null) as
                            OperatorFieldModelController;
                    if (ofm != null)
                    {
                        foreach (KeyValuePair<Key, TypeInfo> inputKey in ofm.Inputs)
                        {
                            foreach (KeyValuePair<Key, TypeInfo> outputKey in ofm.Outputs)
                            {
                                var irfm =
                                    new DocumentFieldReference(docVM.DocumentController.GetId(), inputKey.Key);
                                var orfm =
                                    new DocumentFieldReference(docVM.DocumentController.GetId(), outputKey.Key);
                                //Graph.AddEdge(irfm.DereferenceToRoot().GetId(),
                                //    orfm.DereferenceToRoot().GetId());
                            }
                        }
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var eOldItem in e.OldItems)
                {
                    var docVM = eOldItem as DocumentViewModel;
                    Debug.Assert(docVM != null);
                    OperatorFieldModelController ofm =
                        docVM.DocumentController.GetDereferencedField(OperatorDocumentModel.OperatorKey, null) as
                            OperatorFieldModelController;
                    if (ofm != null)
                    {
                        foreach (KeyValuePair<Key, TypeInfo> inputKey in ofm.Inputs)
                        {
                            foreach (KeyValuePair<Key, TypeInfo> outputKey in ofm.Outputs)
                            {
                                var irfm =
                                    new DocumentFieldReference(docVM.DocumentController.GetId(), inputKey.Key);
                                var orfm =
                                    new DocumentFieldReference(docVM.DocumentController.GetId(), outputKey.Key);
                                //Graph.RemoveEdge(irfm.DereferenceToRoot(null).GetId(),
                                    //orfm.DereferenceToRoot(null).GetId());
                            }
                        }
                    }
                }
            }
        }

        private void ItemsControl_ItemsChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            //RefreshItemsBinding();
            if (e.CollectionChange == CollectionChange.ItemInserted)
            {
                var docVM = sender[(int)e.Index] as DocumentViewModel;
                Debug.Assert(docVM != null);
                OperatorFieldModelController ofm = docVM.DocumentController.GetDereferencedField(OperatorDocumentModel.OperatorKey, null) as OperatorFieldModelController;
                if (ofm != null)
                {
                    foreach (KeyValuePair<Key, TypeInfo> inputKey in ofm.Inputs)
                    {
                        foreach (KeyValuePair<Key, TypeInfo> outputKey in ofm.Outputs)
                        {
                            var irfm = new DocumentFieldReference(docVM.DocumentController.GetId(), inputKey.Key);
                            var orfm = new DocumentFieldReference(docVM.DocumentController.GetId(), outputKey.Key);
                            Graph.AddEdge(irfm.DereferenceToRoot(null).GetId(), orfm.DereferenceToRoot(null).GetId());
                        }
                    }
                }
            }
            //else if (e.CollectionChange == CollectionChange.ItemRemoved)
            //{
            //    var docVM = sender[(int)e.Index] as DocumentViewModel;
            //    Debug.Assert(docVM != null);
            //    OperatorFieldModelController ofm = docVM.DocumentController.GetField(OperatorDocumentModel.OperatorKey) as OperatorFieldModelController;
            //    if (ofm != null)
            //    {
            //        foreach (var inputKey in ofm.InputKeys)
            //        {
            //            foreach (var outputKey in ofm.OutputKeys)
            //            {
            //                ReferenceFieldModel irfm = new ReferenceFieldModel(docVM.DocumentController.GetId(), inputKey);
            //                ReferenceFieldModel orfm = new ReferenceFieldModel(docVM.DocumentController.GetId(), outputKey);
            //                _graph.RemoveEdge(irfm, orfm);
            //            }
            //        }
            //    }
            //}
        }

        //private void Grid_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        //{
        //    if (e.Container is ScrollBar || e.Container is ScrollViewer)
        //    {
        //        e.Complete();
        //        e.Handled = true;
        //    }
        //}

        private void DocumentViewContainerGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Thickness border = DocumentViewContainerGrid.BorderThickness;
            ClipRect.Rect = new Rect(border.Left, border.Top, e.NewSize.Width - border.Left * 2, e.NewSize.Height - border.Top * 2);
        }

        /// <summary>
        /// Make sure the canvas is still in bounds after resize
        /// </summary>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TranslateTransform translate = new TranslateTransform();

            //Calculate bottomRight corner of screen in canvas space before and after resize 
            Debug.Assert(DocumentViewContainerGrid.RenderTransform != null);
            Debug.Assert(DocumentViewContainerGrid.RenderTransform.Inverse != null);
            Point oldBottomRight =
                DocumentViewContainerGrid.RenderTransform.Inverse.TransformPoint(new Point(e.PreviousSize.Width, e.PreviousSize.Height));
            Point bottomRight =
                DocumentViewContainerGrid.RenderTransform.Inverse.TransformPoint(new Point(e.NewSize.Width, e.NewSize.Height));

            //Check if new bottom right is out of bounds
            bool outOfBounds = false;
            if (bottomRight.X > Grid.ActualWidth - 1)
            {
                translate.X = -(oldBottomRight.X - bottomRight.X);
                outOfBounds = true;
            }
            if (bottomRight.Y > Grid.ActualHeight - 1)
            {
                translate.Y = -(oldBottomRight.Y - bottomRight.Y);
                outOfBounds = true;
            }
            //If it is out of bounds, translate so that is is in bounds
            if (outOfBounds)
            {
                TransformGroup composite = new TransformGroup();
                composite.Children.Add(translate);
                composite.Children.Add(DocumentViewContainerGrid.RenderTransform);
                DocumentViewContainerGrid.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            }

            Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
        }

        #region Operator connection stuff
        /// <summary>
        /// Helper class to detect cycles 
        /// </summary>
        private Graph<string> _graph = new Graph<string>();
        /// <summary>
        /// Line to create and display connection lines between OperationView fields and Document fields 
        /// </summary>
        private Path _connectionLine;

        private MultiBinding<PathFigureCollection> _lineBinding;

        /// <summary>
        /// IOReference (containing reference to fields) being referred to when creating the visual connection between fields 
        /// </summary>
        private OperatorView.IOReference _currReference;

        private Dictionary<ReferenceFieldModelController, Path> _lineDict = new Dictionary<ReferenceFieldModelController, Path>();

        /// <summary>
        /// HashSet of current pointers in use so that the OperatorView does not respond to multiple inputs 
        /// </summary>
        private HashSet<uint> _currentPointers = new HashSet<uint>();

        #endregion

        private void ConnectionEllipse_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        private void ConnectionEllipse_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
            OperatorView.IOReference ioRef = new OperatorView.IOReference(new DocumentFieldReference(docId, outputKey), true, e, el, ParentDocument);
            CollectionView view = ParentCollection;
            (view.CurrentView as CollectionFreeformView)?.StartDrag(ioRef);
        }

        private void ConnectionEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
            OperatorView.IOReference ioRef = new OperatorView.IOReference(new DocumentFieldReference(docId, outputKey), false, e, el, ParentDocument);
            CollectionView view = ParentCollection;
            (view.CurrentView as CollectionFreeformView)?.EndDrag(ioRef);
        }

        public void xGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            MainPage.Instance.MainDocView.DragOver -= MainPage.Instance.xCanvas_DragOver;
            ItemsCarrier carrier = ItemsCarrier.GetInstance();
            carrier.Source = ViewModel;
            foreach (var item in e.Items)
                carrier.Payload.Add(item as DocumentViewModel);
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        public void xGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (args.DropResult == DataPackageOperation.Move && !ViewModel.KeepItemsOnMove)
                ChangeDocuments(ItemsCarrier.GetInstance().Payload, false);
            //RefreshItemsBinding();
            ViewModel.KeepItemsOnMove = true;
            var carrier = ItemsCarrier.GetInstance();
            carrier.Payload.Clear();
            carrier.Source = null;
            carrier.Destination = null;
            carrier.Translate = new Point();
            MainPage.Instance.MainDocView.DragOver += MainPage.Instance.xCanvas_DragOver;
        }

        private void ChangeDocuments(List<DocumentViewModel> docViewModels, bool add)
        {
            var docControllers = docViewModels.Select(item => item.DocumentController);
            var controller = ViewModel.CollectionFieldModelController;
            if (controller != null)
                foreach (var item in docControllers)
                    if (add) controller.AddDocument(item);
                    else controller.RemoveDocument(item);
        }

        private void CollectionGrid_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if(ItemsCarrier.GetInstance().Source != ViewModel)
                e.AcceptedOperation = DataPackageOperation.Move;
        }

        private async void CollectionGrid_Drop(object sender, DragEventArgs e)
        {
            
            if (e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null)
            {
                var action =
                    e.DataView.Properties[RadialMenuView.RadialMenuDropKey] as Action<CollectionView, DragEventArgs>;
                if (action != null)
                {
                    action.Invoke(this, e);
                    e.Handled = true;
                }
                    
                return;
            }
            e.Handled = true;
            RefreshItemsBinding();
            foreach (var s in e.DataView.AvailableFormats)
                Debug.Write("" + s);
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    foreach (var i in items)
                        if (i is Windows.Storage.StorageFile)
                        {
                            var storageFile = i as Windows.Storage.StorageFile;
                            if (storageFile.ContentType.Contains("image"))
                            {
                                var bitmapImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                                bitmapImage.SetSource(await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read));
                                var doc = new AnnotatedImage(new Uri(i.Path), i.Name);
                                (DataContext as CollectionViewModel).CollectionFieldModelController.AddDocument(doc.Document);
                            }
                        }
                }
            }
            else if (ItemsCarrier.GetInstance().Source != null)
            {
                //var text = await e.DataView.GetTextAsync(StandardDataFormats.Html).AsTask();
                ItemsCarrier.GetInstance().Destination = ViewModel;
                ItemsCarrier.GetInstance().Source.KeepItemsOnMove = false;
                ItemsCarrier.GetInstance().Translate = CurrentView is CollectionFreeformView 
                                                        ? e.GetPosition(((CollectionFreeformView) CurrentView).xItemsControl.ItemsPanelRoot) 
                                                        : new Point();
                ChangeDocuments(ItemsCarrier.GetInstance().Payload, true);
            }
        }

        private void RefreshItemsBinding()
        {
            var gridView = CurrentView as CollectionGridView;
            var listView = CurrentView as CollectionListView;
            if (gridView != null)
            {
                gridView.xGridView.ItemsSource = null;
                gridView.xGridView.ItemsSource = ViewModel.DataBindingSource;
            }
            else if (listView != null)
            {
                listView.HListView.ItemsSource = null;
                listView.HListView.ItemsSource = ViewModel.DataBindingSource;
            }
        }



        #region Menu
        /// <summary>
        /// Changes the view to the Freeform by making that Freeform visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetFreeformView()
        {
            if (CurrentView is CollectionFreeformView) return;
            ManipulationMode = ManipulationModes.All;
            CurrentView = new CollectionFreeformView();
            (CurrentView as CollectionFreeformView).xItemsControl.Items.VectorChanged += ItemsControl_ItemsChanged;
            xContentControl.Content = CurrentView;
        }
        /// <summary>
        /// Changes the view to the ListView by making that Grid visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetListView()
        {
            if (CurrentView is CollectionListView) return;
            ManipulationMode = ManipulationModes.None;
            CurrentView = new CollectionListView(this);
            (CurrentView as CollectionListView).HListView.SelectionChanged += ViewModel.SelectionChanged;
            xContentControl.Content = CurrentView;
        }
        /// <summary>
        /// Changes the view to the GridView by making that Grid visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetGridView()
        {
            if (CurrentView is CollectionGridView) return;
            ManipulationMode = ManipulationModes.None;
            CurrentView = new CollectionGridView(this);
            (CurrentView as CollectionGridView).xGridView.SelectionChanged += ViewModel.SelectionChanged;
            xContentControl.Content = CurrentView;
        }

        private void MakeSelectionModeMultiple()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Multiple;
            ViewModel.CanDragItems = true;
            _colMenu.GoToDocumentMenu();
        }

        private void CloseMenu()
        {
            var panel = _colMenu.Parent as Panel;
            if (panel != null) panel.Children.Remove(_colMenu);
            _colMenu = null;
            xMenuColumn.Width = new GridLength(0);
            //Temporary graphical hax. to be removed when collectionview menu moved to its document.
            //ParentDocument.xBorder.Margin = new Thickness(ParentDocument.xBorder.Margin.Left - 50,
            //                                                ParentDocument.xBorder.Margin.Top,
            //                                                ParentDocument.xBorder.Margin.Right,
            //                                                ParentDocument.xBorder.Margin.Bottom);
        }

        private void SelectAllItems()
        {
            if (CurrentView is CollectionGridView)
            {
                var gridView = (CurrentView as CollectionGridView).xGridView;
                if (gridView.SelectedItems.Count != ViewModel.DataBindingSource.Count)
                    gridView.SelectAll();
                else gridView.SelectedItems.Clear();
            }
            if (CurrentView is CollectionListView)
            {
                var listView = (CurrentView as CollectionListView).HListView;
                if (listView.SelectedItems.Count != ViewModel.DataBindingSource.Count)
                    listView.SelectAll();
                else
                    listView.SelectedItems.Clear();
            }
        }

        private void MakeSelectionModeSingle()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Single;
            _colMenu.BackToCollectionMenu();
        }

        private void DeleteSelection()
        {
            ViewModel.DeleteSelected_Tapped(null, null);
        }

        private void DeleteCollection()
        {
            ParentDocument.DeleteDocument();
        }

        private void OpenMenu()
        {
            var multipleSelection = new Action(MakeSelectionModeMultiple);
            var deleteSelection = new Action(DeleteSelection);
            var singleSelection = new Action(MakeSelectionModeSingle);
            var selectAll = new Action(SelectAllItems);
            var setGrid = new Action(SetGridView);
            var setList = new Action(SetListView);
            var setFreeform = new Action(SetFreeformView);
            var deleteCollection = new Action(DeleteCollection);

            var collectionButtons = new List<MenuButton>()
            {
                new MenuButton(Symbol.TouchPointer, "Select", Colors.SteelBlue, multipleSelection)
                {
                    RotateOnTap = true
                },
                new MenuButton(Symbol.ViewAll, "Grid", Colors.SteelBlue, setGrid),
                new MenuButton(Symbol.List, "List", Colors.SteelBlue, setList),
                new MenuButton(Symbol.View, "Freeform", Colors.SteelBlue, setFreeform),
                new MenuButton(Symbol.Camera, "ScrCap", Colors.SteelBlue, new Action(ScreenCap)),
                new MenuButton(Symbol.Page, "Json", Colors.SteelBlue, new Action(GetJson))
            };

            if (ParentDocument != MainPage.Instance.MainDocView)
                collectionButtons.Add(new MenuButton(Symbol.Delete, "Delete", Colors.SteelBlue, deleteCollection));

            var documentButtons = new List<MenuButton>()
            {
                new MenuButton(Symbol.Back, "Back", Colors.SteelBlue, singleSelection)
                {
                    RotateOnTap = true
                },
                new MenuButton(Symbol.Edit, "Interface", Colors.SteelBlue, null),
                new MenuButton(Symbol.SelectAll, "All", Colors.SteelBlue, selectAll),
                new MenuButton(Symbol.Delete, "Delete", Colors.SteelBlue, deleteSelection),
            };
            _colMenu = new OverlayMenu(collectionButtons, documentButtons);
            xMenuCanvas.Children.Add(_colMenu);
            xMenuColumn.Width = new GridLength(50);
        }


        #endregion

        public void GetJson()
        {
            throw new NotImplementedException("The document view model does not have a context any more");
            //Util.ExportAsJson(ViewModel.DocumentContext.DocContextList); 
        }
        public void ScreenCap()
        {
            Util.ExportAsImage(xOuterGrid);
        }

        #region Collection Activation

        public void CollectionView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ParentDocument != null && ParentDocument.ViewModel.IsInInterfaceBuilder) return;
            if (ParentDocument != null && ParentDocument.ViewModel.DocumentController.DocumentType == MainPage.MainDocumentType)
            {
                SetSelectedElement(null);
                e.Handled = true;
                return;
            }
            if (ParentSelectionElement?.IsSelected != null && ParentSelectionElement.IsSelected)
            {
                OnSelected();
                e.Handled = true;
            }
        }

        #endregion

        /// <summary>
        /// Retiles the BG
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            xBackgroundTileContainer.Children.Clear();
            new ManipulationControls(xBackgroundTileContainer);
            var width = 150;
            var height = 150;
            for (double x = 0; x < Grid.ActualWidth; x += width)
            {
                for (double y = 0; y < Grid.ActualHeight; y += height)
                {
                    var image = new Image { Source = xTileSource.Source };
                    image.Height = height;
                    image.Width = width;
                    image.Opacity = .67;
                    image.Stretch = Stretch.Fill;
                    Canvas.SetLeft(image, x);
                    Canvas.SetTop(image, y);
                    xBackgroundTileContainer.Children.Add(image);
                }
            }
            xBackgroundClip.Rect = new Rect(0,0, e.NewSize.Width, e.NewSize.Height);
        }

        protected override void OnActivated(bool isSelected)
        {
            if (isSelected)
            {
                CurrentView.IsHitTestVisible = true;
            }
            else
            {
                CurrentView.IsHitTestVisible = false;
                ViewModel.ItemSelectionMode = ListViewSelectionMode.None;
                ViewModel.CanDragItems = false;
            }
        }

        public override void OnLowestActivated(bool isLowestSelected)
        {
            if(_colMenu == null && isLowestSelected) OpenMenu();
            else if (_colMenu != null && ParentDocument.ViewModel.DocumentController.DocumentType != MainPage.MainDocumentType) CloseMenu();
        }

        private void CollectionView_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            (CurrentView as CollectionFreeformView)?.UserControl_ManipulationDelta(sender, e);
        }

        private void CollectionView_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            (CurrentView as CollectionFreeformView)?.UserControl_PointerWheelChanged(sender, e);
        }
    }
}
