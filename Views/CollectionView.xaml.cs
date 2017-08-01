﻿using System;
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
using Windows.UI.Xaml.Controls.Primitives;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : SelectionElement
    {

        public int MaxZ { get; set; }


        //TODO move to freeform view
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

        // TODO not sure what these are used for and whether or not they should be defined here
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

        public UserControl CurrentView { get; set; }

        // TODO generate overlay menu for collections on the fly
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

        private MenuFlyout _flyout;

        public CollectionView(CollectionViewModel vm)
        {
            InitializeComponent();
            InitializeFlyout();
            ViewModel = vm;
            CurrentView = new CollectionFreeformView();
            xContentControl.Content = CurrentView;
            SetEventHandlers();
            CanLink = true;
        }

        // TODO create and remove this on the fly
        private void InitializeFlyout()
        {
            _flyout = new MenuFlyout();
            var menuItem = new MenuFlyoutItem {Text = "Add Operators"};
            menuItem.Click += MenuItem_Click;
            _flyout.Items?.Add(menuItem);

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var xCanvas = MainPage.Instance.xCanvas;
            if(!xCanvas.Children.Contains(OperatorSearchView.Instance))
                xCanvas.Children.Add(OperatorSearchView.Instance);

            // set the operator menu to the current location of the flyout
            var menu = sender as MenuFlyoutItem;
            var transform = menu.TransformToVisual(MainPage.Instance.xCanvas);
            var pointOnCanvas = transform.TransformPoint(new Point());

            // reset the render transform on the operator search view
            OperatorSearchView.Instance.RenderTransform = new TranslateTransform();

            

            var floatBorder = OperatorSearchView.Instance.SearchView.GetFirstDescendantOfType<Border>();
            if (floatBorder != null)
            {
                Canvas.SetLeft(floatBorder, 0);
                Canvas.SetTop(floatBorder, 0);
            }
            Canvas.SetLeft(OperatorSearchView.Instance, pointOnCanvas.X - 250);
            Canvas.SetTop(OperatorSearchView.Instance, pointOnCanvas.Y);
            OperatorSearchView.AddsToThisCollection = this;
        }

        // TODO make all event handlers in xaml or add them here but not both
        private void SetEventHandlers()
        {
            Loaded += CollectionView_Loaded;
            DocumentViewContainerGrid.DragOver += CollectionGrid_DragOver;
            DocumentViewContainerGrid.Drop += CollectionGrid_Drop;
            ConnectionEllipse.ManipulationStarted += ConnectionEllipse_OnManipulationStarted;
            Tapped += CollectionView_Tapped;
        }

        private void CollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>();
            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();

            // TODO fix this bug, and the shadow thing we were talking about
            if (ParentDocument == MainPage.Instance.MainDocView)
            {
                ParentDocument.IsMainCollection = true;
                //Temporary graphical hax.to be removed when collectionview menu moved to its document.
                ParentDocument.XGrid.Background = new SolidColorBrush(Colors.Transparent);
                ParentDocument.xBorder.Margin = new Thickness(ParentDocument.xBorder.Margin.Left + 5,
                    ParentDocument.xBorder.Margin.Top + 5,
                    ParentDocument.xBorder.Margin.Right,
                    ParentDocument.xBorder.Margin.Bottom);
                OpenMenu(); // TODO generate this on the fly
                ParentSelectionElement?.SetSelectedElement(this);
                xOuterGrid.BorderThickness = new Thickness(0);
            }
        }

        private void DocumentViewContainerGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var borderThickness = DocumentViewContainerGrid.BorderThickness;
            ClipRect.Rect = new Rect(borderThickness.Left, 
                borderThickness.Top, 
                e.NewSize.Width - borderThickness.Left * 2, 
                e.NewSize.Height - borderThickness.Top * 2);
        }

        #region Operator connection stuff

        /// <summary>
        /// Line to create and display connection lines between OperationView fields and Document fields 
        /// </summary>
        private Path _connectionLine;

        private MultiBinding<PathFigureCollection> _lineBinding;

        /// <summary>
        /// IOReference (containing reference to fields) being referred to when creating the visual connection between fields 
        /// </summary>
        private IOReference _currReference;

        private void ConnectionEllipse_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        private void ConnectionEllipse_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
            IOReference ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), true, e, el, ParentDocument); // TODO KB 
            CollectionView view = ParentCollection;
            (view.CurrentView as CollectionFreeformView)?.StartDrag(ioRef);
        }

        private void ConnectionEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
            IOReference ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), false, e, el, ParentDocument); // TODO KB 
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
            var isGridView = CurrentView as CollectionGridView;
            var isListView = CurrentView as CollectionListView;
            if (isGridView != null)
            {
                isGridView.xGridView.ItemsSource = null;
                isGridView.xGridView.ItemsSource = ViewModel.DataBindingSource;
            }
            else if (isListView != null)
            {
                isListView.HListView.ItemsSource = null;
                isListView.HListView.ItemsSource = ViewModel.DataBindingSource;
            }
        }

        #endregion




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
            xContentControl.Content = CurrentView;
        }

        public void SetListView()
        {
            if (CurrentView is CollectionListView) return;
            ManipulationMode = ManipulationModes.None;
            CurrentView = new CollectionListView(this);
            (CurrentView as CollectionListView).HListView.SelectionChanged += ViewModel.SelectionChanged;
            xContentControl.Content = CurrentView;
        }

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

        private void GetJson()
        {
            throw new NotImplementedException("The document view model does not have a context any more");
            //Util.ExportAsJson(ViewModel.DocumentContext.DocContextList); 
        }

        private void ScreenCap()
        {
            Util.ExportAsImage(xOuterGrid);
        }

        #endregion

        #region Collection Activation

        private void CollectionView_Tapped(object sender, TappedRoutedEventArgs e)
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
        #endregion


        private void CollectionView_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            (CurrentView as CollectionFreeformView)?.UserControl_PointerWheelChanged(sender, e);
        }

        private void CollectionView_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            var thisUi = this as UIElement;
            var position = e.GetPosition(thisUi);
            _flyout.ShowAt(thisUi, new Point(position.X, position.Y));
        }
    }
}
