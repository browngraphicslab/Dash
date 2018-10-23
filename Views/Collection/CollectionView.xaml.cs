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
        public enum CollectionViewType { Freeform, Grid, Page, DB, Stacking, Schema, TreeView, Timeline, Graph, Icon }

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

            if (vm.ViewType == CollectionViewType.Freeform)
            {
                vm.ContainerDocument.SetField<TextController>(KeyStore.CollectionOpenViewTypeKey, CollectionViewType.Freeform.ToString(), true);
                vm.ContainerDocument.SetField<NumberController>(KeyStore.CollectionOpenWidthKey, vm.ContainerDocument.GetWidth(), true);
                vm.ContainerDocument.SetField<NumberController>(KeyStore.CollectionOpenHeightKey, vm.ContainerDocument.GetHeight(), true);
                vm.ViewType = CollectionViewType.Icon;
            }
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
            if (args.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                docview.ManipulationMode = ManipulationModes.All;
                CurrentView.UserControl.ManipulationMode = SelectionManager.IsSelected(docview) ||
                this.GetFirstAncestorOfType<DocumentView>().IsTopLevel() ?
                    ManipulationModes.All : ManipulationModes.None;
                args.Handled = true;
            }
            else
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

            SetView(_viewType);
            #endregion
        }

        public void SetupContextMenu(MenuFlyout contextMenu)
        {
            // add another horizontal separator
            contextMenu.Items.Add(new MenuFlyoutSeparator());
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Collection",
                FontWeight = Windows.UI.Text.FontWeights.Bold
            });
            contextMenu.Items.Add(new MenuFlyoutSeparator());

            var unfrozen = ViewModel.DocumentViewModels.FirstOrDefault()?.AreContentsHitTestVisible == true;
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = unfrozen ? "Freeze Contents" : "Unfreeze Contents",
                Icon = new FontIcons.FontAwesome { Icon = unfrozen ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += (ss, ee) => FreezeContents(!unfrozen);

            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Iconify",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMinimize }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += (ss, ee) => Iconify();
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Buttonize",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMinimize }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += (ss, ee) => Buttonize();

            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            contextMenu.Items.Add(new MenuFlyoutSubItem()
            {
                Text = "View As",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Eye }
            });
            foreach (var n in Enum.GetValues(typeof(CollectionViewType)).Cast<CollectionViewType>())
            {
                (contextMenu.Items.Last() as MenuFlyoutSubItem).Items.Add(new MenuFlyoutItem() { Text = n.ToString() });
                ((contextMenu.Items.Last() as MenuFlyoutSubItem).Items.Last() as MenuFlyoutItem).Click += (ss, ee) =>
                {
                    using (UndoManager.GetBatchHandle())
                    {
                        SetView(n);
                    }
                };
            }
            CurrentView?.SetupContextMenu(contextMenu);


            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = ViewModel.ContainerDocument.GetFitToParent() ? "Make Unbounded" : "Fit to Parent",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMaximize }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += ParentDocumentView.MenuFlyoutItemFitToParent_Click;

            // add a horizontal separator in context menu
            contextMenu.Items.Add(new MenuFlyoutSeparator());
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Scripting",
                FontWeight = Windows.UI.Text.FontWeights.Bold
            });
            contextMenu.Items.Add(new MenuFlyoutSeparator());

            // add the item to create a repl
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Create Scripting REPL",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Code }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += ReplFlyout_OnClick;

            // add the item to create a scripting view
            contextMenu.Items.Add(new MenuFlyoutItem()
            {
                Text = "Create Script Editor",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.WindowMaximize }
            });
            (contextMenu.Items.Last() as MenuFlyoutItem).Click += ScriptEdit_OnClick;
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
        public void Iconify()
        {
            SetView(CollectionViewType.Icon);
            ViewModel.ContainerDocument.SetWidth(double.NaN);
            ViewModel.ContainerDocument.SetHeight(double.NaN);
        }
        public void FreezeContents(bool unfrozen)
        {
            foreach (var child in ViewModel.DocumentViewModels)
            {
                child.AreContentsHitTestVisible = unfrozen;
            }
        }
        public void Buttonize()
        {
            var newdoc = new RichTextNote(ViewModel.ContainerDocument.Title,
                ViewModel.ContainerDocument.GetPosition() ?? new Point()).Document;
            newdoc.Link(ViewModel.ContainerDocument, LinkBehavior.Follow, "Button");
            newdoc.SetIsButton(true);
            var thisView = this.GetFirstAncestorOfType<DocumentView>();
            thisView.ParentCollection?.ViewModel.AddDocument(newdoc);
            thisView.DeleteDocument();
        }
        public void SetView(CollectionViewType viewType)
        {
            var initialViewType = _viewType;
            _viewType = viewType;
            if (CurrentView?.UserControl != null)
                CurrentView.UserControl.Loaded -= CurrentView_Loaded;
            switch (_viewType)
            {
            case CollectionViewType.Freeform:
                if (CurrentView is CollectionFreeformView) return;
                CurrentView = new CollectionFreeformView();
                break;
            case CollectionViewType.Icon:
                if (CurrentView is CollectionIconView) return;
                if (CurrentView != null && CurrentView.ViewModel.ViewType != CollectionViewType.Icon)
                {
                    ViewModel.ContainerDocument.SetField<TextController>(KeyStore.CollectionOpenViewTypeKey, CurrentView.ViewModel.ViewType.ToString(), true);
                    ViewModel.ContainerDocument.SetField<NumberController>(KeyStore.CollectionOpenWidthKey, ViewModel.ContainerDocument.GetWidth(), true);
                    ViewModel.ContainerDocument.SetField<NumberController>(KeyStore.CollectionOpenHeightKey, ViewModel.ContainerDocument.GetHeight(), true);
                }
                CurrentView = new CollectionIconView();
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

            if (initialViewType == CollectionViewType.Icon && CurrentView?.ViewModel?.ViewType != CollectionViewType.Icon)
            {
                ViewModel.ContainerDocument.SetWidth(ViewModel.ContainerDocument.GetField<NumberController>(KeyStore.CollectionOpenWidthKey)?.Data ?? double.NaN);
                ViewModel.ContainerDocument.SetHeight(ViewModel.ContainerDocument.GetField<NumberController>(KeyStore.CollectionOpenHeightKey)?.Data ?? double.NaN);
            }

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
