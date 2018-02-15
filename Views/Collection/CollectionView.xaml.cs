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

using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using System.Numerics;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Views.Document_Menu;
using Windows.System;
using Windows.UI.Core;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl
    {
        public int MaxZ { get; set; }
        
        public UserControl CurrentView { get; set; }
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

        public enum CollectionViewType  { Freeform, Grid, Page, DB, Schema, TreeView, Timeline }

        CollectionViewType _viewType;

        public CollectionView(CollectionViewModel vm, CollectionViewType viewType = CollectionViewType.Freeform)
        {
            Loaded += CollectionView_Loaded;
            InitializeComponent();
            _viewType = viewType;
            ViewModel = vm;

            Unloaded += CollectionView_Unloaded;

            PointerPressed += OnPointerPressed;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            var forceDrag = (args.KeyModifiers & VirtualKeyModifiers.Shift) == 0 && (args.GetCurrentPoint(this).Properties.IsRightButtonPressed || Window.Current.CoreWindow
                                   .GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down));
            if (forceDrag && this.GetFirstAncestorOfType<CollectionFreeformView>() != null)
            {
                new ManipulationControlHelper(this, xOuterGrid, args.Pointer, false); // manipulate the top-most collection view
                
                args.Handled = true;
            }
        }

        public void TryBindToParentDocumentSize()
        {
            Util.ForceBindHeightToParentDocumentHeight(this);
        }

        public DocumentViewModel GetDocumentViewModel(DocumentController document)
        {
            foreach (var dv in ViewModel.DocumentViewModels)
            {
                if (dv.DocumentController.Equals(document))
                    return dv;
            }
            return null;
        }

        public DocumentController GetDocumentGroup(DocumentController document)
        {
            if (ParentDocument == null)
                return null;
            var groupsList = ParentDocument.ViewModel.DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);

            if (groupsList == null) return null;
            foreach (var g in groupsList.TypedData)
            {
                if (g.Equals(document))
                {
                    return null;
                }
                else
                {
                    var cfield = g.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                    if (cfield != null && cfield.Data.Where((cd) => (cd as DocumentController).Equals(document)).Count() > 0)
                    {
                        return g;
                    }
                }
            }
            return null;
        }

        #region Load And Unload Initialization and Cleanup

        private void CollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CollectionView_Loaded;
            Unloaded -= CollectionView_Unloaded;
        }

        private void CollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            CompoundFreeform = this.GetFirstAncestorOfType<CompoundOperatorEditor>();  // in case the collection is added to a compoundoperatorview 
            ParentDocument.StyleCollection(this);

            UpdateContextMenu();


            // set the top-level viewtype to be freeform by default
            SetView(ParentDocument == MainPage.Instance.MainDocView ? CollectionViewType.Freeform : _viewType);

            // TODO remove this arbtirary styling here
            if (ParentDocument == MainPage.Instance.MainDocView)
            {
                ParentDocument.IsMainCollection = true;
                xOuterGrid.BorderThickness = new Thickness(0);
                //CurrentView.InitializeAsRoot();
            }
        }



        #endregion

        #region CollectionView context menu 

        /// <summary>
        /// This method will update the right-click context menu from the DocumentView with the items in the CollectionView (with options to add new document/collection, and to 
        /// view the collection as different formats).
        /// </summary>
        void UpdateContextMenu()
        {

            var elementsToBeRemoved = new List<MenuFlyoutItemBase>();

            // add a horizontal separator in context menu
            var contextMenu = ParentDocument.MenuFlyout;
            var separatorOne = new MenuFlyoutSeparator();
            contextMenu.Items.Add(separatorOne);
            elementsToBeRemoved.Add(separatorOne);

            // add the item to create a new document
            var newDocument = new MenuFlyoutItem() { Text = "Add new document", Icon = new FontIcon() { Glyph = "\uf016;", FontFamily = new FontFamily("Segoe MDL2 Assets") } };
            newDocument.Click += MenuFlyoutItemNewDocument_Click;
            contextMenu.Items.Add(newDocument);
            elementsToBeRemoved.Add(newDocument);

            // add the item to create a new collection
            var newCollection = new MenuFlyoutItem() { Text = "Add new collection", Icon = new FontIcon() { Glyph = "\uf247;", FontFamily = new FontFamily("Segoe MDL2 Assets") } };
            newCollection.Click += MenuFlyoutItemNewCollection_Click;
            contextMenu.Items.Add(newCollection);
            elementsToBeRemoved.Add(newCollection);

            var tagMode = new MenuFlyoutItem() {Text = "Tag Notes"};

            void EnterTagMode(object sender, RoutedEventArgs e)
            {
                tagMode.Click -= EnterTagMode;
                tagMode.Click += ExitTagMode;

                tagMode.Text = "Exit Tag Mode";

                var view = CurrentView as CollectionFreeformView;
                view?.ShowTagKeyBox();
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
                    view.HideTagKeyBox();
                    view.TagMode = false;
                }
                
            }


            tagMode.Click += EnterTagMode;
            contextMenu.Items.Add(tagMode);
            elementsToBeRemoved.Add(tagMode);

            // add another horizontal separator
            var separatorTwo = new MenuFlyoutSeparator();
            contextMenu.Items.Add(separatorTwo);
            elementsToBeRemoved.Add(separatorTwo);

            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            var viewCollectionAs = new MenuFlyoutSubItem() { Text = "View Collection As" };
            contextMenu.Items.Add(viewCollectionAs);
            elementsToBeRemoved.Add(viewCollectionAs);

            var freeform = new MenuFlyoutItem() { Text = CollectionViewType.Freeform.ToString() };
            freeform.Click += MenuFlyoutItemView_Click;
            viewCollectionAs.Items.Add(freeform);

            var grid = new MenuFlyoutItem() { Text = CollectionViewType.Grid.ToString() };
            grid.Click += MenuFlyoutItemView_Click;
            viewCollectionAs.Items.Add(grid);

            var browse = new MenuFlyoutItem() { Text = CollectionViewType.Page.ToString() };
            browse.Click += MenuFlyoutItemView_Click;
            viewCollectionAs.Items.Add(browse);

            var db = new MenuFlyoutItem() { Text = CollectionViewType.DB.ToString() };
            db.Click += MenuFlyoutItemView_Click;
            viewCollectionAs.Items.Add(db);

            var schema = new MenuFlyoutItem() { Text = CollectionViewType.Schema.ToString() };
            schema.Click += MenuFlyoutItemView_Click;
            viewCollectionAs.Items.Add(schema);

            var timeline = new MenuFlyoutItem() { Text = CollectionViewType.Timeline.ToString() };
            timeline.Click += MenuFlyoutItemView_Click;
            viewCollectionAs.Items.Add(timeline);


            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            var viewCollectionPreview = new MenuFlyoutItem() { Text = "Preview" };
            viewCollectionPreview.Click += ParentDocument.MenuFlyoutItemPreview_Click;
            contextMenu.Items.Add(viewCollectionPreview);
            elementsToBeRemoved.Add(viewCollectionPreview);

            Unloaded += (sender, args) =>
            {
                foreach (var flyoutItem in elementsToBeRemoved)
                {
                    contextMenu.Items.Remove(flyoutItem);
                }
            };

        }
        #endregion

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

        void MenuFlyoutItemView_Click(object sender, RoutedEventArgs e)
        {
            SetView((CollectionView.CollectionViewType)Enum.Parse(typeof(CollectionView.CollectionViewType), (sender as MenuFlyoutItem).Text));
        }

        #endregion

        #region Menu
        public void SetView(CollectionViewType viewType)
        {
            _viewType = viewType;
            switch (_viewType)
            {
                case CollectionViewType.Freeform:
                    if (CurrentView is CollectionFreeformView) return;
                    CurrentView = new CollectionFreeformView();
                    break;
                case CollectionViewType.Grid:
                    if (CurrentView is CollectionGridView) return;
                    CurrentView = new CollectionGridView();
                    break;
                case CollectionViewType.Page:
                    if (CurrentView is CollectionPageView) return;
                    CurrentView = new CollectionPageView();
                    break;
                case CollectionViewType.DB:
                    if (CurrentView is CollectionDBView) return;
                    CurrentView = new CollectionDBView();
                    break;
                case CollectionViewType.Schema:
                    if (CurrentView is CollectionDBSchemaView) return;
                    CurrentView = new CollectionDBSchemaView();
                    break;
                case CollectionViewType.TreeView:
                    break;
                case CollectionViewType.Timeline:
                    if (CurrentView is CollectionTimelineView) return;
                    CurrentView = new CollectionTimelineView();
                    break;
                default:
                    throw new NotImplementedException("You need to add support for your collectionview here");
            }
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.LayoutDocument?.SetField(KeyStore.CollectionViewTypeKey, new TextController(viewType.ToString()), true);
        }

        public void MakeSelectionModeMultiple()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Multiple;
            ViewModel.CanDragItems = true;

            if (CurrentView is CollectionFreeformView)
            {
                (CurrentView as CollectionFreeformView).IsSelectionEnabled = true;
            }
        }
        private void MakeSelectionModeSingle()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Single;
            ViewModel.CanDragItems = true;
        }

        private void MakeSelectionModeNone()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.None;
            ViewModel.CanDragItems = false;

            if (CurrentView is CollectionFreeformView)
            {
                (CurrentView as CollectionFreeformView).IsSelectionEnabled = false;
            }
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

        #endregion
    }
}
