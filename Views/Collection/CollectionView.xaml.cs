using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.System;
using Windows.UI;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl, ICollectionView
    {
        public enum CollectionViewType { Freeform, Grid, Page, DB, Schema, TreeView, Timeline }

        CollectionViewType _viewType;
        public int MaxZ { get; set; }
        public UserControl CurrentView { get; set; }
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel;  }

        /// <summary>
        /// The <see cref="CollectionView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>(); 

        /// <summary>
        /// The <see cref="DocumentView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public DocumentView ParentDocument => this.GetFirstAncestorOfType<DocumentView>();

        public CollectionView(CollectionViewModel vm, CollectionViewType viewType = CollectionViewType.Freeform)
        {
            Loaded += CollectionView_Loaded;
            InitializeComponent();
            _viewType = viewType;
            DataContext = vm;

            Unloaded += CollectionView_Unloaded;
            DragLeave += (sender, e) => ViewModel.CollectionViewOnDragLeave(sender, e);
            DragEnter += (sender, e) => ViewModel.CollectionViewOnDragEnter(sender, e);
            DragOver += (sender, e) => ViewModel.CollectionViewOnDragOver(sender, e);
            Drop += (sender, e) => ViewModel.CollectionViewOnDrop(sender, e);

            PointerPressed += OnPointerPressed;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            var shifted = (args.KeyModifiers & VirtualKeyModifiers.Shift) != 0;
            var rightBtn = args.GetCurrentPoint(this).Properties.IsRightButtonPressed;
            var parentFreeform = this.GetFirstAncestorOfType<CollectionFreeformView>();
            if (parentFreeform != null && rightBtn)
            {
                var parentParentFreeform = parentFreeform.GetFirstAncestorOfType<CollectionFreeformView>();
                var grabbed = parentParentFreeform == null && (args.KeyModifiers & VirtualKeyModifiers.Shift) != 0 && args.OriginalSource != this;
                if (!grabbed && (shifted || parentParentFreeform == null))
                {
                    new ManipulationControlHelper(this, args.Pointer, true); // manipulate the top-most collection view

                    args.Handled = true;
                } else 
                    if (parentParentFreeform != null)
                        CurrentView.ManipulationMode = ManipulationModes.None;
            }
        }

        #region Load And Unload Initialization and Cleanup

        private void CollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CollectionView_Loaded;
            Unloaded -= CollectionView_Unloaded;
        }

        private void CollectionView_Loaded(object s, RoutedEventArgs args)
        { 
            ParentDocument.StyleCollection(this);
            
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

                // add the item to create a new collection
                var newCollection = new MenuFlyoutItem() { Text = "Add new collection", Icon = new FontIcon() { Glyph = "\uf247;", FontFamily = new FontFamily("Segoe MDL2 Assets") } };
                newCollection.Click += (sender, e) =>
                {
                    var pt = Util.GetCollectionFreeFormPoint(CurrentView as CollectionFreeformView, GetFlyoutOriginCoordinates());
                    ViewModel.AddDocument(Util.BlankCollectionWithPosition(pt), null); //NOTE: Because mp is null when in, for example, grid view, this will do nothing
                };
                contextMenu.Items.Add(newCollection);
                elementsToBeRemoved.Add(newCollection);

                var tagMode = new MenuFlyoutItem() { Text = "Tag Notes" };

                void EnterTagMode(object sender, RoutedEventArgs e)
                {
                    tagMode.Click -= EnterTagMode;
                    tagMode.Click += ExitTagMode;

                    tagMode.Text = "Exit Tag Mode";
                    
                    (CurrentView as CollectionFreeformView)?.ShowTagKeyBox();
                }

                void ExitTagMode(object sender, RoutedEventArgs e)
                {
                    tagMode.Click -= ExitTagMode;
                    tagMode.Click += EnterTagMode;

                    tagMode.Text = "Tag Notes";
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

                foreach (var n in Enum.GetValues(typeof(CollectionViewType)).Cast<CollectionViewType>())
                {
                    var vtype = new MenuFlyoutItem() { Text = n.ToString() };
                    vtype.Click += (sender, e) => SetView(n);
                    viewCollectionAs.Items.Add(vtype);
                }


                // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
                var viewCollectionPreview = new MenuFlyoutItem() { Text = "Preview" };
                viewCollectionPreview.Click += ParentDocument.MenuFlyoutItemPreview_Click;
                contextMenu.Items.Add(viewCollectionPreview);
                elementsToBeRemoved.Add(viewCollectionPreview);

                Unloaded += (sender, e) =>
                {
                    foreach (var flyoutItem in elementsToBeRemoved)
                    {
                        contextMenu.Items.Remove(flyoutItem);
                    }
                };

            }
            #endregion
            UpdateContextMenu();

            // set the top-level viewtype to be freeform by default
            SetView(ParentDocument == MainPage.Instance.MainDocView ? CollectionViewType.Freeform : _viewType);
        }
        
        #endregion
        
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

        #endregion

        #region Menu
        public void SetView(CollectionViewType viewType)
        {
            _viewType = viewType;
            switch (_viewType)
            {
                case CollectionViewType.Freeform:
                    if (CurrentView is CollectionFreeformView) return;
                    CurrentView = new CollectionFreeformView() { InkController = ViewModel.InkController };
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
            var curViewType = ParentDocument?.ViewModel?.LayoutDocument?.GetDereferencedField<TextController>(KeyStore.CollectionViewTypeKey, null)?.Data;
            if (curViewType != _viewType.ToString())
                ParentDocument?.ViewModel?.LayoutDocument?.SetField(KeyStore.CollectionViewTypeKey, new TextController(viewType.ToString()), true);
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

        public void Highlight()
        {
            xOuterGrid.BorderBrush = new SolidColorBrush(Color.FromArgb(102, 255, 215, 0));
        }

        public void Unhighlight()
        {
            xOuterGrid.BorderBrush = new SolidColorBrush(Colors.Transparent);
        }
    }
}
