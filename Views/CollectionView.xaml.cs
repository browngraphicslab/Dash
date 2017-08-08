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
    public sealed partial class CollectionView : UserControl
    {
        public int MaxZ { get; set; }
        public SelectionElement CurrentView { get; set; }
        private OverlayMenu _collectionMenu;
        public CollectionViewModel ViewModel
        {
            get { return DataContext as CollectionViewModel; }
            set { DataContext = value; }
        }
        public CollectionView ParentCollection { get; set; }
        public DocumentView ParentDocument { get; set; }


        public CollectionView(CollectionViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            ViewModel.OnLowestSelectionSet += OnLowestSelectionSet;

            Loaded += CollectionView_Loaded;
            Unloaded += CollectionView_Unloaded;
        }

        #region Load And Unload Initialization and Cleanup

        private void CollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CollectionView_Loaded;
            Unloaded -= CollectionView_Unloaded;
            ViewModel.OnLowestSelectionSet -= OnLowestSelectionSet;
        }

        private void CollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>();
            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();

            CurrentView = new CollectionFreeformView();
            xContentControl.Content = CurrentView;

            if (ParentDocument == MainPage.Instance.MainDocView)
            {
                ParentDocument.IsMainCollection = true;
                xOuterGrid.BorderThickness = new Thickness(0);
                CurrentView.InitializeAsRoot();
            }
        }

        #endregion

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
            string docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            KeyController outputKey = DocumentCollectionFieldModelController.CollectionKey;
            IOReference ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), true, e, el, ParentDocument); // TODO KB 
            CollectionView view = ParentCollection;
            (view.CurrentView as CollectionFreeformView)?.StartDrag(ioRef);
        }

        private void ConnectionEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            KeyController outputKey = DocumentCollectionFieldModelController.CollectionKey;
            IOReference ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), false, e, el, ParentDocument); // TODO KB 
            CollectionView view = ParentCollection;
            (view.CurrentView as CollectionFreeformView)?.EndDrag(ioRef);
        }

        #endregion

        #region Menu
        private void SetFreeformView()
        {
            if (CurrentView is CollectionFreeformView) return;
            CurrentView = new CollectionFreeformView();
            xContentControl.Content = CurrentView;
        }

        private void SetListView()
        {
            if (CurrentView is CollectionListView) return;
            CurrentView = new CollectionListView();
            xContentControl.Content = CurrentView;
        }

        private void SetGridView()
        {
            if (CurrentView is CollectionGridView) return;
            CurrentView = new CollectionGridView();
            xContentControl.Content = CurrentView;
        }

        private void MakeSelectionModeMultiple()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Multiple;
            ViewModel.CanDragItems = true;
            _collectionMenu.GoToDocumentMenu();
        }

        private void CloseMenu()
        {
            // if the collection menu was already closed then return
            //if (_collectionMenu == null) return;

            var panel = _collectionMenu.Parent as Panel;
            panel?.Children.Remove(_collectionMenu);
            //_collectionMenu.Dispose();
            //_collectionMenu = null;
            xMenuColumn.Width = new GridLength(0);
        }

        private void SelectAllItems()
        {
            var view = CurrentView as ICollectionView;
            Debug.Assert(view != null, "make the view implement ICollectionView");
            view.ToggleSelectAllItems();
        }

        private void MakeSelectionModeSingle()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Single;
            ViewModel.CanDragItems = true;
            _collectionMenu.BackToCollectionMenu();
        }

        private void MakeSelectionModeNone()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.None;
            ViewModel.CanDragItems = false;
            _collectionMenu.BackToCollectionMenu();
        }

        private void DeleteSelection()
        {
            ViewModel.DeleteSelected_Tapped();
        }

        private void DeleteCollection()
        {
            ParentDocument.DeleteDocument();
        }

        private void MakeMenu()
        {
            var multipleSelection = new Action(MakeSelectionModeMultiple);
            var deleteSelection = new Action(DeleteSelection);
            var singleSelection = new Action(MakeSelectionModeSingle);
            var noSelection = new Action(MakeSelectionModeNone);
            var selectAll = new Action(SelectAllItems);
            var setGrid = new Action(SetGridView);
            var setList = new Action(SetListView);
            var setFreeform = new Action(SetFreeformView);
            var deleteCollection = new Action(DeleteCollection);

            var menuColor = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]).Color;


            var collectionButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.TouchPointer, "Select", menuColor, multipleSelection)
                {
                    RotateOnTap = true
                },
                //toggle grid/list/freeform view buttons 
                new MenuButton(new List<Symbol> { Symbol.ViewAll, Symbol.List, Symbol.View}, menuColor, new List<Action> { setGrid, setList, setFreeform}),
                new MenuButton(Symbol.Camera, "ScrCap", menuColor, new Action(ScreenCap)),
                new MenuButton(Symbol.Page, "Json", menuColor, new Action(GetJson)),
            };

            if (ParentDocument != MainPage.Instance.MainDocView)
                collectionButtons.Add(new MenuButton(Symbol.Delete, "Delete", menuColor, deleteCollection));

            var documentButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.Back, "Back", menuColor, noSelection)
                {
                    RotateOnTap = true
                },
                new MenuButton(Symbol.Edit, "Interface", menuColor, null),
                new MenuButton(Symbol.SelectAll, "All", menuColor, selectAll),
                new MenuButton(Symbol.Delete, "Delete", menuColor, deleteSelection),
            };
            _collectionMenu = new OverlayMenu(collectionButtons, documentButtons);
        }

        private void OpenMenu()
        {
            if (_collectionMenu == null) MakeMenu();
            if (xMenuCanvas.Children.Contains(_collectionMenu)) return;
            xMenuCanvas.Children.Add(_collectionMenu);
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

        private void OnLowestSelectionSet(bool isLowestSelected)
        {
            // if we're the lowest selected then open the menu
            if (isLowestSelected)
            {
                OpenMenu();
            }

            // if we are no longer the lowest selected and we are not the main collection then close the menu
            if (isLowestSelected == false && ParentDocument.IsMainCollection == false)
            {
                CloseMenu();
            }
        }

        #endregion
    }
}
