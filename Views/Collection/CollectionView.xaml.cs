using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Dash.FontIcons;
using System.Diagnostics;
using Windows.Devices.Input;
using Dash.Views.Collection;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl, ICollectionView
    {
        public UserControl UserControl => this;
        public enum CollectionViewType { Freeform, Grid, Page, DB, Stacking, Schema, TreeView, Timeline, Graph }

        CollectionViewModel _lastViewModel = null;
        CollectionViewType  _viewType;

        public int MaxZ { get; set; }
        public ICollectionView CurrentView { get; set; }
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }

        /// <summary>
        /// The <see cref="CollectionView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>();

        /// <summary>
        /// The <see cref="DocumentView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public DocumentView ParentDocumentView => this.GetFirstAncestorOfType<DocumentView>();

        public event Action<object, RoutedEventArgs> CurrentViewLoaded;

        public CollectionView(CollectionViewModel vm)
        {
            Loaded += CollectionView_Loaded;
            Unloaded += CollectionView_Unloaded;
            InitializeComponent();

            DataContext = vm;
            _lastViewModel = vm;
            SetView(vm.ViewType);
            DragLeave += (sender, e) => ViewModel.CollectionViewOnDragLeave(sender, e);
            DragEnter += (sender, e) => ViewModel.CollectionViewOnDragEnter(sender, e);
            DragOver += (sender, e) => ViewModel.CollectionViewOnDragOver(sender, e);
            Drop += (sender, e) => ViewModel.CollectionViewOnDrop(sender, e);
            id = COLid++;

            xOuterGrid.PointerPressed += OnPointerPressed;
            var color = xOuterGrid.Background;
        }

        ~CollectionView()
        {
            Debug.WriteLine("Finalizing CollectionView");
        }

        /// <summary>
        /// Begins panning events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            var docview = this.GetFirstAncestorOfType<DocumentView>();
            //if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            //{
            //    if (!SelectionManager.IsSelected(docview))
            //        SelectionManager.Select(docview, false);
            //    SelectionManager.TryInitiateDragDrop(docview, args, null);
            //}
            if (args.GetCurrentPoint(this).Properties.IsRightButtonPressed ) 
            {
                docview.ManipulationMode = ManipulationModes.All;
                CurrentView.UserControl.ManipulationMode = SelectionManager.IsSelected(docview) ||
                this.GetFirstAncestorOfType<DocumentView>().IsTopLevel() ?
                    ManipulationModes.All : ManipulationModes.None;
                    args.Handled = true;
            } else
            {
                docview.ManipulationMode = ManipulationModes.None;
            }
        }

        private int count = 0;
        private static int COLid = 0;
        private int id = 0;
        private void CollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"CollectionView {id} unloaded {--count}");
            _lastViewModel?.Loaded(false);
            RemoveViewTypeHandler();
        }

        private void CollectionView_Loaded(object s, RoutedEventArgs args)
        {
            //Debug.WriteLine($"CollectionView {id} loaded : {++count}");
            _lastViewModel = ViewModel;
            ViewModel.Loaded(true);
            AddViewTypeHandler();

            // ParentDocument can be null if we are rendering collections for thumbnails
            if (ParentDocumentView == null)
            {
                SetView(_viewType);
                return;
            }

            var cp = ParentDocumentView.GetFirstDescendantOfType<CollectionView>();
            if (cp != this)
                return;

            #region CollectionView context menu 


            var contextMenu = ParentDocumentView.MenuFlyout;
            if (!contextMenu.Items.OfType<MenuFlyoutItem>().Select((mfi) => mfi.Text).Contains("Create Scripting REPL")) {

                var elementsToBeRemoved = new List<MenuFlyoutItemBase>();

                // add a horizontal separator in context menu
                elementsToBeRemoved.Add(new MenuFlyoutSeparator());

                // add the item to create a repl
                elementsToBeRemoved.Add(new MenuFlyoutItem()
                {
                    Text = "Create Scripting REPL",
                    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Code }
                });
                (elementsToBeRemoved.Last() as MenuFlyoutItem).Click += ReplFlyout_OnClick;

                // add the item to create a scripting view
                elementsToBeRemoved.Add(new MenuFlyoutItem()
                {
                    Text = "Create Script Editor",
                    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMaximize }
                });
                (elementsToBeRemoved.Last() as MenuFlyoutItem).Click += ScriptEdit_OnClick;

                // add another horizontal separator
                elementsToBeRemoved.Add(new MenuFlyoutSeparator());

                // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
                elementsToBeRemoved.Add(new MenuFlyoutSubItem()
                {
                    Text = "View Collection As",
                    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Eye }
                });

                foreach (var n in Enum.GetValues(typeof(CollectionViewType)).Cast<CollectionViewType>())
                {
                     (elementsToBeRemoved.Last() as MenuFlyoutSubItem).Items.Add(new MenuFlyoutItem() { Text = n.ToString() });
                    ((elementsToBeRemoved.Last() as MenuFlyoutSubItem).Items.Last() as MenuFlyoutItem).Click += (ss, ee) => { using (UndoManager.GetBatchHandle()) SetView(n); };
                }

                // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
                elementsToBeRemoved.Add(new MenuFlyoutItem()
                {
                    Text = "Toggle Fit To Parent",
                    Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMaximize }
                });
                (elementsToBeRemoved.Last() as MenuFlyoutItem).Click += ParentDocumentView.MenuFlyoutItemFitToParent_Click;

                elementsToBeRemoved.ForEach((ele) => contextMenu.Items.Add(ele));
                Unloaded += (sender, e) => elementsToBeRemoved.ForEach((ele) => contextMenu.Items.Remove(ele));
            }

            SetView(_viewType);
        #endregion
        }

        private void ScriptEdit_OnClick(object sender, RoutedEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(CurrentView as CollectionFreeformBase, GetFlyoutOriginCoordinates());
            var note = new DishScriptBox(@where.X, @where.Y).Document;
            Actions.DisplayDocument(ViewModel, note, @where);
        }

        private void ReplFlyout_OnClick(object sender, RoutedEventArgs e)
        {
            Point where = Util.GetCollectionFreeFormPoint(CurrentView as CollectionFreeformBase, GetFlyoutOriginCoordinates());
            DocumentController note = new DishReplBox(@where.X, @where.Y, 300, 400).Document;
            Actions.DisplayDocument(ViewModel, note, @where);
        }


        #region ClickHandlers for collection context menu items

        /// <summary>
        /// Gets the screen coordinates of the top left corner of the first flyout item.
        /// </summary>
        /// <returns></returns>
        private Point GetFlyoutOriginCoordinates()
        {
            var firstFlyoutItem = ParentDocumentView.MenuFlyout.Items.FirstOrDefault();
            return Util.PointTransformFromVisual(new Point(), firstFlyoutItem);
        }

        #endregion

        private void AddViewTypeHandler()
        {
            ViewModel?.ContainerDocument.AddFieldUpdatedListener(KeyStore.CollectionViewTypeKey, ViewTypeHandler);
        }

        private void ViewTypeHandler(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            SetView(ViewModel.ViewType);
        }

        private void RemoveViewTypeHandler()
        {
            ViewModel?.ContainerDocument.RemoveFieldUpdatedListener(KeyStore.CollectionViewTypeKey, ViewTypeHandler);
        }

        #region Menu
        public void SetView(CollectionViewType viewType)
        {
            _viewType = viewType;
            if (CurrentView?.UserControl != null)
                CurrentView.UserControl.Loaded -= CurrentView_Loaded;
            switch (_viewType)
            {
            case CollectionViewType.Freeform:
                if (CurrentView is CollectionFreeformView) return;
                CurrentView = new CollectionFreeformView();
                break;
            case CollectionViewType.Stacking:
                if (CurrentView is CollectionStackView) return;
                CurrentView = new CollectionStackView();
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
                if (CurrentView is CollectionTreeView) return;
                CurrentView = new CollectionTreeView();
                break;
            case CollectionViewType.Timeline:
                if (CurrentView is CollectionTimelineView) return;
                CurrentView = new CollectionTimelineView();
                break;
            case CollectionViewType.Graph:
                if (CurrentView is CollectionGraphView) return;
                CurrentView = new CollectionGraphView();
                break;
            default:
                throw new NotImplementedException("You need to add support for your collectionview here");
            }
            CurrentView.UserControl.Loaded -= CurrentView_Loaded;
            CurrentView.UserControl.Loaded += CurrentView_Loaded;

            xContentControl.Content = CurrentView;
            if (ViewModel.ViewType != _viewType)
                ViewModel.ViewType = viewType;
        }

        private void CurrentView_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentViewLoaded?.Invoke(sender, e);
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
        public void SetBorderThickness(double thickness)
        {
            this.xOuterGrid.BorderThickness = new Thickness(thickness);
        }

        public void Highlight()
        {
            xOuterGrid.BorderBrush = new SolidColorBrush(Color.FromArgb(102, 255, 215, 0));
        }

        public void Unhighlight()
        {
            xOuterGrid.BorderBrush = new SolidColorBrush(Colors.Transparent);
        }

        public void SetDropIndicationFill(Brush fill) { CurrentView?.SetDropIndicationFill(fill); }
    }
}
