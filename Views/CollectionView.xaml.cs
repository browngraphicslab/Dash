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
using Windows.UI.Xaml.Controls.Primitives;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : SelectionElement
    {
        public int MaxZ { get; set; }
        public UserControl CurrentView { get; set; }
        private OverlayMenu _colMenu;
        public CollectionViewModel ViewModel
        {
            get { return DataContext as CollectionViewModel;}
            set { DataContext = value; }
        }
        public CollectionView ParentCollection { get; set; }
        public DocumentView ParentDocument { get; set; }


        public CollectionView(CollectionViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            CurrentView = new CollectionFreeformView();
            xContentControl.Content = CurrentView;
        }



        private void CollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>();

            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            if (ParentDocument == MainPage.Instance.MainDocView)
            {

                ParentDocument.IsMainCollection = true;
                ParentSelectionElement?.SetSelectedElement(this);
                xOuterGrid.BorderThickness = new Thickness(0);
            }
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
            string docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
            IOReference ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), true, e, el, ParentDocument); // TODO KB 
            CollectionView view = ParentCollection;
            (view.CurrentView as CollectionFreeformView)?.StartDrag(ioRef);
        }

        private void ConnectionEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
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
            CurrentView = new CollectionListView(ViewModel);
            // TODO see if these methods can be abstracted
            ((CollectionListView) CurrentView).HListView.SelectionChanged += ViewModel.SelectionChanged;
            xContentControl.Content = CurrentView;
        }

        private void SetGridView()
        {
            if (CurrentView is CollectionGridView) return;
            CurrentView = new CollectionGridView(ViewModel);
            // TODO see if these methods can be abstracted
            ((CollectionGridView) CurrentView).xGridView.SelectionChanged += ViewModel.SelectionChanged;
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
            panel?.Children.Remove(_colMenu);
            _colMenu.Dispose();
            _colMenu = null;
            xMenuColumn.Width = new GridLength(0);
        }

        private void SelectAllItems()
        {
            var view = CurrentView as CollectionGridView;
            if (view != null)
            {
                var gridView = view.xGridView;
                if (gridView.SelectedItems.Count != ViewModel.DocumentViewModels.Count)
                    gridView.SelectAll();
                else gridView.SelectedItems.Clear();
            }
            var currentView = CurrentView as CollectionListView;
            if (currentView != null)
            {
                var listView = currentView.HListView;
                if (listView.SelectedItems.Count != ViewModel.DocumentViewModels.Count)
                    listView.SelectAll();
                else
                    listView.SelectedItems.Clear();
            }
        }

        private void MakeSelectionModeSingle()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Single;
            ViewModel.CanDragItems = true;
            _colMenu.BackToCollectionMenu();
        }

        private void MakeSelectionModeNone()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.None;
            ViewModel.CanDragItems = false;
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
            else if (_colMenu != null && !isLowestSelected && ParentDocument.ViewModel.DocumentController.DocumentType != MainPage.MainDocumentType) CloseMenu();
        }


        #endregion


    }
}
