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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl, ICollectionView
    {
        public UserControl UserControl => this;
        public enum CollectionViewType { Freeform, Grid, Page, DB, Schema, TreeView, Timeline, Graph, Standard }

        CollectionViewType _viewType;
        public int MaxZ { get; set; }
        public ICollectionView CurrentView { get; set; }
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel;  }

        /// <summary>
        /// The <see cref="CollectionView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>(); 

        /// <summary>
        /// The <see cref="DocumentView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public DocumentView ParentDocument => this.GetFirstAncestorOfType<DocumentView>();

        public event Action<object, RoutedEventArgs> CurrentViewLoaded;

        CollectionViewModel _lastViewModel = null;
        public CollectionView(CollectionViewModel vm)
        {
            Loaded += CollectionView_Loaded;
            InitializeComponent();
            _viewType = vm.ViewType;
            DataContext = vm;
            _lastViewModel = vm;
            Unloaded += CollectionView_Unloaded;
            DragLeave += (sender, e) => ViewModel.CollectionViewOnDragLeave(sender, e);
            DragEnter += (sender, e) => ViewModel.CollectionViewOnDragEnter(sender, e);
            DragOver += (sender, e) => ViewModel.CollectionViewOnDragOver(sender, e);
            Drop += (sender, e) => ViewModel.CollectionViewOnDrop(sender, e);

            DocumentViewContainerGrid.PointerPressed += OnPointerPressed;
	        var color = xOuterGrid.Background;
        }

        /// <summary>
        /// Begins panning events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            var shifted = (args.KeyModifiers & VirtualKeyModifiers.Shift) != 0;
            var rightBtn = args.GetCurrentPoint(this).Properties.IsRightButtonPressed;
            var parentFreeform = this.GetFirstAncestorOfType<CollectionFreeformBase>();
            if (parentFreeform != null && rightBtn)
            {
                var parentParentFreeform = parentFreeform.GetFirstAncestorOfType<CollectionFreeformBase>();
                var grabbed = parentParentFreeform == null && (args.KeyModifiers & VirtualKeyModifiers.Shift) != 0 && args.OriginalSource != this;
                if (!grabbed && (shifted || parentParentFreeform == null))
                {
                    new ManipulationControlHelper(this, args.Pointer, true); // manipulate the top-most collection view

                    args.Handled = true;
                }
                else
                    if (parentParentFreeform != null)
                        CurrentView.UserControl.ManipulationMode = ManipulationModes.None;
            }
            

        }

        private void CollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            _lastViewModel?.Loaded(false);
            _lastViewModel = null;
        }

        private void CollectionView_Loaded(object s, RoutedEventArgs args)
        {
            _lastViewModel = ViewModel;
            ViewModel.Loaded(true);

	       // var docView = this.GetFirstAncestorOfType<DocumentView>();

            // ParentDocument can be null if we are rendering collections for thumbnails
            if (ParentDocument == null)
            {
                SetView(_viewType);
                return;
            }

            ParentDocument.StyleCollection(this);

            #region CollectionView context menu 

            /// <summary>
            /// Update the right-click context menu from the DocumentView with the items in the CollectionView (with options to add new document/collection, and to 
            /// view the collection as different formats).
            /// </summary>
            var elementsToBeRemoved = new List<MenuFlyoutItemBase>();

            // add a horizontal separator in context menu
            var contextMenu = ParentDocument.MenuFlyout;
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

            foreach (CollectionViewType n in Enum.GetValues(typeof(CollectionViewType)).Cast<CollectionViewType>())
            {
                var vtype = new MenuFlyoutItem() {Text = n.ToString()};

                void VTypeOnClick(object sender, RoutedEventArgs e)
                {
                    using (UndoManager.GetBatchHandle())
                        SetView(n);
                }

                vtype.Click += VTypeOnClick;
                //vtype.Unloaded += delegate { vtype.Click -= VType_OnClick; };
                viewCollectionAs.Items?.Add(vtype);
            }

            // add the outer SubItem to "View collection as" to the context menu, and then add all the different view options to the submenu 
            var fitToParent = new MenuFlyoutItem() {Text = "Toggle Fit To Parent"};
            fitToParent.Click += ParentDocument.MenuFlyoutItemFitToParent_Click;
            var icon4 = new FontIcons.FontAwesome
            {
                Icon = FontAwesomeIcon.WindowMaximize
            };
            fitToParent.Icon = icon4;
            contextMenu.Items.Add(fitToParent);
            elementsToBeRemoved.Add(fitToParent);

            //ParentDocument.Unloaded += delegate
            //{
            //    viewCollectionPreview.Click -= ParentDocument.MenuFlyoutItemFitToParent_Click;
            //    fitToParent.Click -= ParentDocument.MenuFlyoutItemFitToParent_Click;
            //};

            Unloaded += (sender, e) =>
            {
                foreach (var flyoutItem in elementsToBeRemoved)
                {
                    contextMenu.Items.Remove(flyoutItem);
                }
                
                newRepl.Click -= ReplFlyout_OnClick;
                newScriptEdit.Click -= ScriptEdit_OnClick;
            };

            // set the top-level viewtype to be freeform by default
            if (ParentDocument != MainPage.Instance.MainDocView || _viewType == CollectionViewType.Freeform ||
                _viewType == CollectionViewType.Standard)
            {
                SetView(_viewType);
            }
            else //If we are trying to view the main collection not in a freeform-ish view, force a freeform view
                //TODO This might not be what we want
            {
                SetView(CollectionViewType.Freeform);
            }
        }

        private void ScriptEdit_OnClick(object sender, RoutedEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(CurrentView as CollectionFreeformBase, GetFlyoutOriginCoordinates());
            var note = new DishScriptBox(@where.X, @where.Y, 300, 400).Document;
            Actions.DisplayDocument(ViewModel, note, @where);
        }

        private void ReplFlyout_OnClick(object sender, RoutedEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(CurrentView as CollectionFreeformBase, GetFlyoutOriginCoordinates());
            var note = new DishReplBox(@where.X, @where.Y, 300, 400).Document;
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
            var firstFlyoutItem = ParentDocument.MenuFlyout.Items.FirstOrDefault();
            return Util.PointTransformFromVisual(new Point(), firstFlyoutItem);
        }

        #endregion

        #region Menu
        public void SetView(CollectionViewType viewType)
        {
            if (_viewType.Equals(CollectionViewType.Standard) && !viewType.Equals(CollectionViewType.Standard))
            {
                ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.None;
                this.GetFirstAncestorOfType<DocumentView>().ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.None;
            }
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
                case CollectionViewType.Standard:
                    if (CurrentView is CollectionStandardView) return;
                    CurrentView = new CollectionStandardView();
                    break;
                default:
                    throw new NotImplementedException("You need to add support for your collectionview here");
            }
            CurrentView.UserControl.Loaded -= CurrentView_Loaded;
            CurrentView.UserControl.Loaded += CurrentView_Loaded;
            // tfs - I don't think these three lines are actually doing anything...
            //var selected = SelectionManager.SelectedDocs.ToArray();
            //SelectionManager.DeselectAll();
            //SelectionManager.SelectDocuments(selected.ToList());

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
