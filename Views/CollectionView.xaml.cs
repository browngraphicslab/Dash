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
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.UI.Xaml.Data;

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

        public CompoundOperatorEditor CompoundFreeform { get; set; }

        public DocumentView ParentDocument { get; set; }

        public enum CollectionViewType
        {
            Freeform, List, Grid, Text
        }

        private CollectionViewType _viewType;

        public CollectionView(CollectionViewModel vm, CollectionViewType viewType = CollectionViewType.Freeform)
        {
            InitializeComponent();
            _viewType = viewType;
            ViewModel = vm;
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
            CompoundFreeform = this.GetFirstAncestorOfType<CompoundOperatorEditor>();  // in case the collection is added to a compoundoperatorview 

            switch (_viewType)
            {
                case CollectionViewType.Freeform:
                    CurrentView = new CollectionFreeformView { InkFieldModelController = ViewModel.InkFieldModelController };
                    break;
                case CollectionViewType.Grid:
                    CurrentView = new CollectionGridView();
                    break;
                case CollectionViewType.List:
                    CurrentView = new CollectionListView();
                    break;
                case CollectionViewType.Text:
                    CurrentView = new CollectionTextView();
                    break;
            }
            xContentControl.Content = CurrentView;

            if (ParentDocument == MainPage.Instance.MainDocView)
            {
                ParentDocument.IsMainCollection = true;
                xOuterGrid.BorderThickness = new Thickness(0);
                CurrentView.InitializeAsRoot();
            }

            ViewModel.OnLowestSelectionSet += OnLowestSelectionSet; 
        }

        #endregion

        #region Operator connection stuff
        private void ConnectionEllipse_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        private void ConnectionEllipse_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (ParentCollection == null) return;
            string docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            Ellipse el = ConnectionEllipse;
            KeyController outputKey = ViewModel.CollectionKey;
            IOReference ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), true, TypeInfo.Collection, e, el, ParentDocument);

            CollectionFreeformView freeform = ParentCollection.CurrentView as CollectionFreeformView;
            if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;
            freeform.CanLink = true;
            freeform.StartDrag(ioRef);
        }

        private void ConnectionEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (ParentCollection == null) return;
            string docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            Ellipse el = ConnectionEllipse;
            KeyController outputKey = ViewModel.CollectionKey;
            IOReference ioRef = new IOReference(null, null, new DocumentFieldReference(docId, outputKey), false, TypeInfo.Collection, e, el, ParentDocument);

            CollectionFreeformView freeform = ParentCollection.CurrentView as CollectionFreeformView;
            if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;
            freeform.EndDrag(ioRef, false);
        }

        #endregion

        #region Menu
        private void SetFreeformView()
        {
            if (CurrentView is CollectionFreeformView) return;
            CurrentView = new CollectionFreeformView { InkFieldModelController = ViewModel.InkFieldModelController };
            xContentControl.Content = CurrentView;
        }

        private void SetTextView()
        {
            if (CurrentView is CollectionTextView) return;
            CurrentView = new CollectionTextView();
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

        public void MakeSelectionModeMultiple()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Multiple;
            ViewModel.CanDragItems = true;
            _collectionMenu.GoToDocumentMenu();

            if (CurrentView is CollectionFreeformView)
            {
                (CurrentView as CollectionFreeformView).IsSelectionEnabled = true;
            }
        }

        private void CloseMenu()
        {
            xMenuCanvas.Children.Remove(_collectionMenu);
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

            if (CurrentView is CollectionFreeformView)
            {
                (CurrentView as CollectionFreeformView).IsSelectionEnabled = false;
            }
        }

        private void DeleteSelection()
        {
            ViewModel.DeleteSelected_Tapped();
        }

        private void DeleteCollection()
        {
            ParentDocument.DeleteDocument();
        }

        private int GetMenuIndex() 
        {
            switch (_viewType)
            {
                case CollectionViewType.Freeform:
                    return 2; 
                case CollectionViewType.Grid:
                    return 0; 
                case CollectionViewType.List:
                    return 1; 
                default: return -1; 
            }
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
                new MenuButton(Symbol.TouchPointer, "Select", menuColor, MakeSelectionModeMultiple)
                {
                    RotateOnTap = true
                },
                //toggle grid/list/freeform view buttons 
                new MenuButton(new List<Symbol> { Symbol.ViewAll, Symbol.List, Symbol.View}, menuColor, new List<Action> { SetGridView, SetListView, SetFreeformView}, GetMenuIndex()),
                new MenuButton(Symbol.Camera, "ScrCap", menuColor, new Action(ScreenCap)),

                new MenuButton(Symbol.Page, "Json", menuColor, new Action(GetJson))
            };



            if (ParentDocument != MainPage.Instance.MainDocView)
                collectionButtons.Add(new MenuButton(Symbol.Delete, "Delete", menuColor, DeleteCollection));

            var documentButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.Back, "Back", menuColor, MakeSelectionModeNone)
                {
                    RotateOnTap = true
                },
                new MenuButton(Symbol.Edit, "Interface", menuColor, null),
                new MenuButton(Symbol.SelectAll, "All", menuColor, SelectAllItems),
                new MenuButton(Symbol.Delete, "Delete", menuColor, DeleteSelection),
            };
            _collectionMenu = new OverlayMenu(collectionButtons, documentButtons);
        }

        private void OpenMenu()
        {
            if (_collectionMenu == null) MakeMenu();
            if (xMenuCanvas.Children.Contains(_collectionMenu)) return;
            xMenuCanvas.Children.Add(_collectionMenu);
            xMenuColumn.Width = new GridLength(50);
            _collectionMenu.AddAndPlayOpenAnimation();
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

        public void OnLowestSelectionSet(bool isLowestSelected)
        {
            // if we're the lowest selected then open the menu
            if (isLowestSelected)
            {
                OpenMenu();
            }

            // if we are no longer the lowest selected and we are not the main collection then close the menu
            else if (_collectionMenu != null && !isLowestSelected && ParentDocument.IsMainCollection == false)
            {
                CloseMenu();
            }
        }

        #endregion

        /// <summary>
        /// Binds the hit test visibility of xContentControl to the IsSelected of DocumentVieWModel as opposed to CollectionVieWModel 
        /// in order to make ellipses hit test visible and the rest not 
        /// </summary>
        private void xContentControl_Loaded(object sender, RoutedEventArgs e)           // TODO think up a better way to do this 
        {
            var docView = xOuterGrid.GetFirstAncestorOfType<DocumentView>();
            DocumentViewModel datacontext = docView?.DataContext as DocumentViewModel;

            if (datacontext == null) return;
            var visibilityBinding = new Binding
            {
                Source = datacontext,
                Path = new PropertyPath(nameof(datacontext.IsSelected)) 
            };
            xContentControl.SetBinding(IsHitTestVisibleProperty, visibilityBinding); 
        }
       
    }
}
