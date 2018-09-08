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
using Dash.Views.Collection;
using Windows.UI;
using Dash.FontIcons;
using Windows.UI.Core;
using Dash.Converters;
using System.Diagnostics;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl, ICollectionView
    {
        public UserControl UserControl => this;
        public enum CollectionViewType { None, Freeform, Grid, Page, DB, Schema, TreeView, Timeline, Graph }

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

        //if this or any of its children are selected, it can move
        public bool SelectedCollection;

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
            if (SelectionManager.IsSelected(this.GetFirstAncestorOfType<DocumentView>()) || SelectedCollection ||
                this.GetFirstAncestorOfType<DocumentView>().IsTopLevel())
            {
                //selected, so pan 
                CurrentView.UserControl.ManipulationMode = ManipulationModes.All;
            } else
            {
                //don't pan
                CurrentView.UserControl.ManipulationMode = ManipulationModes.None;
            }
        }

        private int count = 0;
        private static int COLid = 0;
        private int id = 0;
        private void CollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine($"CollectionView {id} unloaded {--count}");
            _lastViewModel?.Loaded(false);
            _lastViewModel = null;
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

            #region CollectionView context menu 

            var elementsToBeRemoved = new List<MenuFlyoutItemBase>();

            // add a horizontal separator in context menu
            var contextMenu = ParentDocumentView.MenuFlyout;
            var separatorOne = new MenuFlyoutSeparator();
            contextMenu.Items.Add(separatorOne);
            elementsToBeRemoved.Add(separatorOne);



            // add the item to create a repl
            var newRepl = new MenuFlyoutItem() {Text = "Create Scripting REPL"};

            var icon5 = new FontIcons.FontAwesome
            {
                Icon = FontAwesomeIcon.Code
            };
            newRepl.Icon = icon5;
            newRepl.Click += ReplFlyout_OnClick;
            contextMenu.Items.Add(newRepl);
            elementsToBeRemoved.Add(newRepl);

            // add the item to create a scripting view
            var newScriptEdit = new MenuFlyoutItem() {Text = "Create Script Editor"};
            var icon6 = new FontIcons.FontAwesome
            {
                Icon = FontAwesomeIcon.WindowMaximize
            };
            newScriptEdit.Icon = icon6;
            newScriptEdit.Click += ScriptEdit_OnClick;
            contextMenu.Items.Add(newScriptEdit);
            elementsToBeRemoved.Add(newScriptEdit);

            // add another horizontal separator
            var separatorThree = new MenuFlyoutSeparator();
            contextMenu.Items.Add(separatorThree);
            elementsToBeRemoved.Add(separatorThree);

            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            var viewCollectionAs = new MenuFlyoutSubItem() {Text = "View Collection As"};
            var icon2 = new FontIcons.FontAwesome
            {
                Icon = FontAwesomeIcon.Eye
            };
            viewCollectionAs.Icon = icon2;
            contextMenu.Items.Add(viewCollectionAs);
            elementsToBeRemoved.Add(viewCollectionAs);

            foreach (var n in Enum.GetValues(typeof(CollectionViewType)).Cast<CollectionViewType>())
            {
                var vtype = new MenuFlyoutItem() {Text = n.ToString()};

                void VTypeOnClick(object sender, RoutedEventArgs e)
                {
                    using (UndoManager.GetBatchHandle())
                        SetView(n);
                }

                vtype.Click += VTypeOnClick;
                viewCollectionAs.Items?.Add(vtype);
            }

            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            var fitToParent = new MenuFlyoutItem() {Text = "Toggle Fit To Parent"};
            fitToParent.Click += ParentDocumentView.MenuFlyoutItemFitToParent_Click;
            var icon4 = new FontIcons.FontAwesome
            {
                Icon = FontAwesomeIcon.WindowMaximize
            };
            fitToParent.Icon = icon4;
            contextMenu.Items.Add(fitToParent);
            elementsToBeRemoved.Add(fitToParent);

            Unloaded += (sender, e) =>
            {
                foreach (var flyoutItem in elementsToBeRemoved)
                {
                    contextMenu.Items.Remove(flyoutItem);
                }

                newRepl.Click -= ReplFlyout_OnClick;
                newScriptEdit.Click -= ScriptEdit_OnClick;
            };

            SetView(_viewType);
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

        private void NewCollectionFlyout_OnClick(object sender, RoutedEventArgs e)
        {
            var pt = Util.GetCollectionFreeFormPoint(CurrentView as CollectionFreeformBase, GetFlyoutOriginCoordinates());
            ViewModel.AddDocument(Util.BlankCollectionWithPosition(pt)); //NOTE: Because mp is null when in, for example, grid view, this will do nothing
        }

        #endregion

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
