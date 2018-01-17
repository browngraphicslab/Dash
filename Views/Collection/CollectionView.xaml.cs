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
using Visibility = Windows.UI.Xaml.Visibility;

using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using System.Numerics;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Views.Document_Menu;

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
            get => DataContext as CollectionViewModel;
            set => DataContext = value;
        }

        /// <summary>
        /// The <see cref="CollectionView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public CollectionView ParentCollection;

        /// <summary>
        /// The <see cref="CompoundOperatorEditor"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public CompoundOperatorEditor CompoundFreeform;

        /// <summary>
        /// The <see cref="DocumentView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public DocumentView ParentDocument => this.GetFirstAncestorOfType<DocumentView>();

        /// <summary>
        /// Used as the key to identify when a drag even has started on the preview button
        /// </summary>
        public static string CollectionPreviewDragKey = "CE1ACD38-FB99-4018-A9B5-0430C9089B14";


        public enum CollectionViewType
        {
            Freeform, List, Grid, Page, Text, DB, Schema, TreeView
        }

        private CollectionViewType _viewType;

        public CollectionView(CollectionViewModel vm, CollectionViewType viewType = CollectionViewType.Freeform)
        {
            Loaded += CollectionView_Loaded;
            InitializeComponent();
            _viewType = viewType;
            ViewModel = vm;

            Unloaded += CollectionView_Unloaded;
        }

        public void TryBindToParentDocumentSize()
        {
            Util.ForceBindHeightToParentDocumentHeight(this);
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
            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            CompoundFreeform = this.GetFirstAncestorOfType<CompoundOperatorEditor>();  // in case the collection is added to a compoundoperatorview 
            ParentDocument.StyleCollection(this);

            if (_collectionMenu == null)
            {
                MakeMenu();

                UpdateContextMenu();
            }

            // set the top-level viewtype to be freeform by default
            if (ParentDocument == MainPage.Instance.MainDocView)
            {
                _viewType = CollectionViewType.Freeform;
            }
            switch (_viewType)
            {
                case CollectionViewType.Freeform:
                    SetFreeformView();
                    break;
                case CollectionViewType.Grid:
                    SetGridView();
                    break;
                case CollectionViewType.Page:
                    SetBrowseView();
                    break;
                case CollectionViewType.DB:
                    SetDBView();
                    break;
                case CollectionViewType.Schema:
                    SetSchemaView();
                    break;
                case CollectionViewType.List:
                    SetListView();
                    break;
                case CollectionViewType.Text:
                    SetTextView();
                    break;
            }

            // TODO remove this arbtirary styling here
            if (ParentDocument == MainPage.Instance.MainDocView)
            {
                ParentDocument.IsMainCollection = true;
                xOuterGrid.BorderThickness = new Thickness(0);
                CurrentView.InitializeAsRoot();
                ConnectionEllipseInput.Visibility = Visibility.Collapsed;
            }

            ViewModel.OnLowestSelectionSet += OnLowestSelectionSet;
        }



        #endregion



        #region CollectionView context menu 

        /// <summary>
        /// This method will update the right-click context menu from the DocumentView with the items in the CollectionView (with options to add new document/collection, and to 
        /// view the collection as different formats).
        /// </summary>
        private void UpdateContextMenu()
        {
            // add a horizontal separator in context menu
            ParentDocument.MenuFlyout.Items.Add(new MenuFlyoutSeparator());

            // add the item to create a new document
            var newDocument = new MenuFlyoutItem() { Text = "Add new document", Icon = new FontIcon() { Glyph = "\uf016;", FontFamily = new FontFamily("Segoe MDL2 Assets") } };
            newDocument.Click += MenuFlyoutItemNewDocument_Click;
            ParentDocument.MenuFlyout.Items.Add(newDocument);

            // add the item to create a new collection
            var newCollection = new MenuFlyoutItem() { Text = "Add new collection", Icon = new FontIcon() { Glyph = "\uf247;", FontFamily = new FontFamily("Segoe MDL2 Assets") } };
            newCollection.Click += MenuFlyoutItemNewCollection_Click;
            ParentDocument.MenuFlyout.Items.Add(newCollection);

            var tagMode = new MenuFlyoutItem() {Text = "Tag Notes"};

            void EnterTagMode(object sender, RoutedEventArgs e)
            {
                tagMode.Click -= EnterTagMode;
                tagMode.Click += ExitTagMode;

                tagMode.Text = "Exit Tag Mode";

                var view = CurrentView as CollectionFreeformView;
                if (view != null)
                {
                    view.TagMode = true;
                }
            }

            void ExitTagMode(object sender, RoutedEventArgs e)
            {
                tagMode.Click -= ExitTagMode;
                tagMode.Click += EnterTagMode;

                tagMode.Text = "Tag Notes";
            ;
                var view = CurrentView as CollectionFreeformView;
                if (view != null)
                {
                    view.TagMode = false;
                }
            }


            tagMode.Click += EnterTagMode;
            ParentDocument.MenuFlyout.Items.Add(tagMode);

            // add another horizontal separator
            ParentDocument.MenuFlyout.Items.Add(new MenuFlyoutSeparator());

            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            var viewCollectionAs = new MenuFlyoutSubItem() { Text = "View collection as" };
            ParentDocument.MenuFlyout.Items.Add(viewCollectionAs);

            var freeform = new MenuFlyoutItem() { Text = "Freeform" };
            freeform.Click += MenuFlyoutItemFreeform_Click;
            viewCollectionAs.Items.Add(freeform);

            var grid = new MenuFlyoutItem() { Text = "Grid" };
            grid.Click += MenuFlyoutItemGrid_Click;
            viewCollectionAs.Items.Add(grid);

            var browse = new MenuFlyoutItem() { Text = "Browse" };
            browse.Click += MenuFlyoutItemBrowse_Click;
            viewCollectionAs.Items.Add(browse);

            var db = new MenuFlyoutItem() { Text = "DB" };
            db.Click += MenuFlyoutItemDB_Click;
            viewCollectionAs.Items.Add(db);

            var schema = new MenuFlyoutItem() { Text = "Schema" };
            schema.Click += MenuFlyoutItemSchema_Click;
            viewCollectionAs.Items.Add(schema);
        }

        /// <summary>
        /// Helper function to add a document controller to the main freeform layout.
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="opController"></param>
        private void addElement(Point screenPoint, DocumentController opController)
        {
            var mp = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;

            // using this as a setter for the transform massive hack - LM
            var _ = new DocumentViewModel(opController)
            {
                GroupTransform = new TransformGroupData(Util.GetCollectionFreeFormPoint(mp, screenPoint), new Point(1, 1))
            };

            if (opController != null)
            {
                //freeForm.ViewModel.AddDocument(opController, null);
                mp?.ViewModel.AddDocument(opController, null); //NOTE: Because mp is null when in, for example, grid view, this will do nothing
            }
        }

        #region ClickHandlers for collection context menu items

        /// <summary>
        /// Gets the screen coordinates of the top left corner of the first flyout item.
        /// </summary>
        /// <returns></returns>
        private Point GetFlyoutOriginCoordinates()
        {
            var firstFlyoutItem = ParentDocument.MenuFlyout.Items.FirstOrDefault();
            return Util.PointTransformFromVisual(new Point(), firstFlyoutItem);
        }

        private void MenuFlyoutItemNewDocument_Click(object sender, RoutedEventArgs e)
        {
            addElement(GetFlyoutOriginCoordinates(), Util.BlankDoc());
        }

        private void MenuFlyoutItemNewCollection_Click(object sender, RoutedEventArgs e)
        {
            addElement(GetFlyoutOriginCoordinates(), Util.BlankCollection());
        }

        private void MenuFlyoutItemFreeform_Click(object sender, RoutedEventArgs e)
        {
            SetFreeformView();
        }

        private void MenuFlyoutItemGrid_Click(object sender, RoutedEventArgs e)
        {
            SetGridView();
        }

        private void MenuFlyoutItemBrowse_Click(object sender, RoutedEventArgs e)
        {
            SetBrowseView();
        }

        private void MenuFlyoutItemDB_Click(object sender, RoutedEventArgs e)
        {
            SetDBView();
        }

        private void MenuFlyoutItemSchema_Click(object sender, RoutedEventArgs e)
        {
            SetSchemaView();
        }

        #endregion

        #endregion




        #region Operator connection output
        private void ConnectionEllipse_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }



        #endregion




        #region Connection input and output 
        private void ConnectionEllipseOutput_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FireEllipseInteraction(sender, e, isInput: false, isPressed: true);
            //if (ParentCollection == null) return;
            //// containing documents docId
            //var docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            //// the ellipse which the link should attatch to
            //var el = (sender as Grid)?.Children[0] as Ellipse;
            //var outputKey = ViewModel.OutputKey ?? ViewModel.CollectionKey;
            //var ioRef = new IOReference(new DocumentFieldReference(docId, outputKey), true, TypeInfo.Collection, e, el, ParentDocument);

            //var freeform = ParentCollection.CurrentView as CollectionFreeformView;
            //if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;
            //freeform.CanLink = true;
            //freeform.StartDrag(ioRef);
        }

        private void ConnectionEllipseOutput_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            FireEllipseInteraction(sender, e, isInput: false, isPressed: false);
        }


        private void ConnectionEllipseInput_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FireEllipseInteraction(sender, e, isInput: true, isPressed: true);
            //if (ParentCollection == null) return;
            //var docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            //var el = ConnectionEllipseInput;
            //var outputKey = /*ViewModel.OutputKey ?? */ ViewModel.CollectionKey;
            //var ioRef = new IOReference(new DocumentFieldReference(docId, outputKey), false, TypeInfo.Collection, e, el, ParentDocument);

            //var freeform = ParentCollection.CurrentView as CollectionFreeformView;
            //if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;
            //freeform.CanLink = true;
            //freeform.StartDrag(ioRef);
        }

        private void ConnectionEllipseInput_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            FireEllipseInteraction(sender, e, isInput: true, isPressed: false);
            //if (ParentCollection == null) return;
            //var docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            //var el = ConnectionEllipseInput;
            //var outputKey = /*ViewModel.OutputKey ?? */ ViewModel.CollectionKey;
            //var ioRef = new IOReference(new DocumentFieldReference(docId, outputKey), false, TypeInfo.Collection, e, el, ParentDocument);

            //var freeform = ParentCollection.CurrentView as CollectionFreeformView;
            //if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;
            //freeform?.EndDrag(ioRef, false);
        }

        private void FireEllipseInteraction(object sender, PointerRoutedEventArgs e, bool isInput, bool isPressed)
        {
            if (ParentCollection == null) return;
            var docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetDataDocument(null).GetId();
            var el = (sender as Grid).Children[0] as Ellipse;
            KeyController refKey;
            if (!isInput)
                refKey = ViewModel.OutputKey ?? ViewModel.CollectionKey;
            else
                refKey = ViewModel.CollectionKey;

            var ioRef = new IOReference(new DocumentFieldReference(docId, refKey), !isInput, TypeInfo.List, e, el, ParentDocument);

            var freeform = ParentCollection.CurrentView as CollectionFreeformView;
            if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;

            freeform.CanLink = isPressed;
            if (!isPressed)
                freeform.EndDrag(ioRef, false);
            else
                freeform.StartDrag(ioRef);
        }

        #endregion

        #region Menu
        private void SetFreeformView()
        {
            if (CurrentView is CollectionFreeformView) return;
            CurrentView = new CollectionFreeformView() { InkController = ViewModel.InkController };
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Freeform.ToString()), true);
            ParentDocument?.ViewModel?.DocumentController?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Freeform.ToString()), true);

            ViewModes?.HighlightAction(SetFreeformView);
        }

        private void SetTextView()
        {
            if (CurrentView is CollectionTextView) return;
            CurrentView = new CollectionTextView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Text.ToString()), true);
            ParentDocument?.ViewModel?.DocumentController?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Text.ToString()), true);

            ViewModes?.HighlightAction(SetTextView);
        }

        public void SetDBView()
        {
            if (CurrentView is CollectionDBView) return;
            CurrentView = new CollectionDBView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.DB.ToString()), true);
            ParentDocument?.ViewModel?.DocumentController?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.DB.ToString()), true);

            ViewModes?.HighlightAction(SetDBView);
        }
        private void SetSchemaView()
        {
            if (CurrentView is CollectionDBSchemaView) return;
            CurrentView = new CollectionDBSchemaView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Schema.ToString()), true);
            ParentDocument?.ViewModel?.DocumentController?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Schema.ToString()), true);

            ViewModes?.HighlightAction(SetSchemaView);
        }

        private void SetListView()
        {
            if (CurrentView is CollectionListView) return;
            CurrentView = new CollectionListView();
            xContentControl.Content = CurrentView;
            ViewModes?.HighlightAction(SetListView);
        }
        private void SetBrowseView()
        {
            if (CurrentView is CollectionPageView) return;
            CurrentView = new CollectionPageView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Page.ToString()), true);
            ParentDocument?.ViewModel?.DocumentController?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Page.ToString()), true);

            ViewModes?.HighlightAction(SetBrowseView);
        }

        private void SetGridView()
        {
            if (CurrentView is CollectionGridView) return;
            CurrentView = new CollectionGridView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Grid.ToString()), true);
            ParentDocument?.ViewModel?.DocumentController?.SetField(KeyStore.CollectionViewTypeKey, new TextController(CollectionViewType.Grid.ToString()), true);

            ViewModes?.HighlightAction(SetGridView);
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

        public MenuButton ViewModes;

        private void MakeMenu()
        {
            var collectionButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.TouchPointer, "Select", MakeSelectionModeMultiple)
                {
                    RotateOnTap = true
                },
                //toggle grid/list/freeform view buttons 
                (ViewModes = new MenuButton(
                    new List<Symbol> { Symbol.ViewAll, Symbol.BrowsePhotos, Symbol.Folder, Symbol.Admin, Symbol.View},
                    new List<Action> { SetGridView, SetBrowseView, SetDBView, SetSchemaView, SetFreeformView})),
                new MenuButton(Symbol.Camera, "ScrCap", ScreenCap)


            };

            // Create preview button with special properties so the user can drag off of it
            var previewButton = new MenuButton(Symbol.Preview, "Preview", null);
            var previewButtonView = previewButton.View;
            previewButtonView.CanDrag = true;
            previewButton.ManipulationMode = ManipulationModes.All;
            previewButton.ManipulationDelta += (s, e) => e.Handled = true;
            previewButton.ManipulationStarted += (s, e) => e.Handled = true;
            previewButtonView.DragStarting += (s, e) =>
            {
                e.Data.RequestedOperation = DataPackageOperation.Link;
                e.Data.Properties.Add(CollectionPreviewDragKey, this);
            };
            previewButtonView.DropCompleted += PreviewButtonView_DropCompleted;
            collectionButtons.Add(previewButton);

            var documentButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.Back, "Back", MakeSelectionModeNone)
                {
                    RotateOnTap = true
                },
                new MenuButton(Symbol.Edit, "Interface", null),
                new MenuButton(Symbol.SelectAll, "All", SelectAllItems),
                new MenuButton(Symbol.Delete, "Delete", DeleteSelection),
            };


            _collectionMenu = new OverlayMenu(collectionButtons, documentButtons);

            // taking out the collection menu now that we have the context menu
            _collectionMenu.Visibility = Visibility.Collapsed;
        }

        private void PreviewButtonView_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            // TODO fill this in
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

        private void EnterCollection()
        {
            var rootFrame = Window.Current.Content as Frame;
            Debug.Assert(rootFrame != null);
            rootFrame.Navigate(typeof(MainPage), ParentDocument.ViewModel.DocumentController);

        }

        #endregion

        #region Collection Activation

        public void OnLowestSelectionSet(bool isLowestSelected)
        {
            // if we're the lowest selected then open the menu
            if (isLowestSelected)
            {
            }

            // if we are no longer the lowest selected and we are not the main collection then close the menu
            else if (_collectionMenu != null && ParentDocument?.IsMainCollection == false)
            {

            }
        }

        #endregion

        /// <summary>
        /// Binds the hit test visibility of xContentControl to the IsSelected of DocumentVieWModel as opposed to CollectionVieWModel 
        /// in order to make ellipses hit test visible and the rest not 
        /// </summary>
        private void xContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO this method is special cased and therfore hard to debug...
            var docView = xOuterGrid.GetFirstAncestorOfType<DocumentView>();
            var datacontext = docView?.DataContext as DocumentViewModel;
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
