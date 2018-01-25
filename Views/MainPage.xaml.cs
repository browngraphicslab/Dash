using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DashShared;
using DashShared.Models;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Dash.Views.Document_Menu;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Visibility = Windows.UI.Xaml.Visibility;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    ///     Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Instance { get; private set; }

        private RadialMenuView _radialMenu;
        private static CollectionView _mainCollectionView;
        private Flyout OperatorMenuFlyout;
        public DocumentView MainDocView { get { return xMainDocView; } set { xMainDocView = value; } }
        public RadialMenuView RadialMenu => _radialMenu;
        public DocumentController MainDocument { get; private set; }
        public static InkController InkController = new InkController();
        public BrowserView WebContext => BrowserView.Current;

        public bool SearchVisible { get; private set; }



        public MainPage()
        {
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            //formattableTitleBar.ButtonBackgroundColor = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]).Color;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = false;

            InitializeComponent();

            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;

            _radialMenu = new RadialMenuView(xCanvas);

            _radialMenu.Loaded += delegate
            {
                _radialMenu.JumpToPosition(3 * ActualWidth / 4, 3 * ActualHeight / 4);
            };
            Loaded += OnLoaded;

            Window.Current.CoreWindow.KeyUp += CoreWindowOnKeyUp;
            Window.Current.CoreWindow.KeyDown += CoreWindowOnKeyDown;

            SearchVisible = false;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            async Task Success(IEnumerable<DocumentModel> mainPages)
            {
                var doc = mainPages.FirstOrDefault();
                if (doc != null)
                {
                    MainDocument = ContentController<FieldModel>.GetController<DocumentController>(doc.Id);
                    if (MainDocument.GetActiveLayout() == null)
                    {
                        var layout =
                            new CollectionBox(
                                new DocumentReferenceController(MainDocument.GetId(), KeyStore.CollectionKey)).Document;
                        MainDocument.SetActiveLayout(layout, true, true);
                    }
                }
                else
                {
                    var fields = new Dictionary<KeyController, FieldControllerBase>
                    {
                        [KeyStore.CollectionKey] = new ListController<DocumentController>(),
                        [KeyStore.GroupingKey] = new ListController<DocumentController>()
                    };
                    MainDocument = new DocumentController(fields, DashConstants.TypeStore.MainDocumentType);
                    var layout = new CollectionBox(new DocumentReferenceController(MainDocument.GetId(), KeyStore.CollectionKey)).Document;
                    MainDocument.SetActiveLayout(layout, true, true);
                }

                var col = MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.CollectionKey);
                var grouped = MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.GroupingKey);
                DocumentController lastWorkspace;
                if (col.Count == 0)
                {
                    //var documentController = new CollectionNote(new Point(0, 0),
                    //    CollectionView.CollectionViewType.Freeform, "New Workspace").Document;
                    var documentController = new NoteDocuments.CollectionNote(new Point(0, 0),
                        CollectionView.CollectionViewType.Freeform).Document;
                    col.Add(documentController);
                    grouped.Add(documentController);
                    MainDocument.SetField(KeyStore.LastWorkspaceKey, documentController, true);
                    lastWorkspace = documentController;
                }
                else
                {
                    lastWorkspace = MainDocument.GetField(KeyStore.LastWorkspaceKey) as DocumentController;
                }
                lastWorkspace.SetWidth(double.NaN);
                lastWorkspace.SetHeight(double.NaN);
                xMainDocView.DataContext = new DocumentViewModel(lastWorkspace);
            }

            await RESTClient.Instance.Fields.GetDocumentsByQuery<DocumentModel>(
                new DocumentTypeLinqQuery(DashConstants.TypeStore.MainDocumentType), Success, ex => throw ex);



            BrowserView.ForceInit();

            //this next line is optional and can be removed.  
            //Its only use right now is to tell the user that there is successful communication (or not) between Dash and the Browser
            BrowserView.OpenTab("https://en.wikipedia.org/wiki/Special:Random");
        }

        public bool SetCurrentWorkspace(DocumentController workspace)
        {
            //prevents us from trying to enter the main document.  Can remove this for further extensibility but it doesn't work yet
            if (workspace.Equals(MainDocument))
            {
                return false;
            }
            workspace = workspace.MakeDelegate();
            workspace.SetWidth(double.NaN);
            workspace.SetHeight(double.NaN);
            var documentViewModel = new DocumentViewModel(workspace);
            xMainDocView.DataContext = documentViewModel;
            documentViewModel.SetSelected(null, true);
            MainDocument.SetField(KeyStore.LastWorkspaceKey, workspace, true);
            return true;
        }

        public void SetCurrentWorkspaceAndNavigateToDocument(DocumentController workspace, DocumentController document)
        {
            RoutedEventHandler handler = null;
            handler =
                delegate(object sender, RoutedEventArgs args)
                {
                    xMainDocView.xContentPresenter.Loaded -= handler;


                    var dvm = xMainDocView.DataContext as DocumentViewModel;
                    var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
                    if (coll?.ViewModel?.DocumentViewModels != null)
                    {
                        foreach (var vm in coll.ViewModel.DocumentViewModels)
                        {
                            if (vm.DocumentController.Equals(document))
                            {
                                RoutedEventHandler finalHandler = null;
                                finalHandler = delegate(object finalSender, RoutedEventArgs finalArgs)
                                {
                                    Debug.WriteLine("loaded");
                                    NavigateToDocumentInWorkspace(document);
                                    vm.Content.Loaded -= finalHandler;
                                };

                                vm.Content.Loaded += finalHandler;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (dvm?.Content != null)
                        {
                            if (coll == null)
                            {
                                RoutedEventHandler contentHandler = null;
                                contentHandler = delegate(object contentSender, RoutedEventArgs contentArgs)
                                {
                                    dvm.Content.Loaded -= contentHandler;
                                    if (!NavigateToDocumentInWorkspace(document))
                                    {
                                        handler(null, null);
                                    }
                                };
                                dvm.Content.Loaded += contentHandler;
                            }
                            else
                            {
                                RoutedEventHandler contentHandler = null;
                                contentHandler = delegate (object contentSender, RoutedEventArgs contentArgs)
                                {
                                    coll.Loaded -= contentHandler;
                                    if (!NavigateToDocumentInWorkspace(document))
                                    {
                                        handler(null, null);
                                    }
                                };
                                coll.Loaded += contentHandler;
                            }
                        }

                    }
                };
            xMainDocView.xContentPresenter.Loaded += handler;
            if (!SetCurrentWorkspace(workspace))
            {
                xMainDocView.xContentPresenter.Loaded -= handler;
            }
        }

        public bool NavigateToDocumentInWorkspace(DocumentController document)
        {
            var dvm = xMainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            if (coll != null)
            {
                return NavigateToDocument(coll, null, coll, document);
            }
            return false;
        }

        public bool NavigateToDocumentInWorkspaceAnimated(DocumentController document)
        {
            var dvm = xMainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            if (coll != null)
            {
                return NavigateToDocumentAnimated(coll, null, coll, document);
            }
            return false;
        }

        public bool NavigateToDocumentAnimated(CollectionFreeformView root, DocumentViewModel rootViewModel, CollectionFreeformView collection, DocumentController document)
        {
            if (collection?.ViewModel?.DocumentViewModels == null)
            {
                return false;
            }

            foreach (var dm in collection.ViewModel.DocumentViewModels)
                if (dm.DocumentController.Equals(document))
                {
                    var containerViewModel = rootViewModel ?? dm;
                    var canvas = root.xItemsControl.ItemsPanelRoot as Canvas;
                    var center = new Point((xMainDocView.ActualWidth - xMainTreeView.ActualWidth) / 2, xMainDocView.ActualHeight / 2);
                    var shift = canvas.TransformToVisual(xMainDocView).TransformPoint(
                        new Point(
                            containerViewModel.GroupTransform.Translate.X + containerViewModel.ActualWidth / 2,
                            containerViewModel.GroupTransform.Translate.Y + containerViewModel.ActualHeight / 2));
                    root.MoveAnimated(new TranslateTransform() { X = center.X - shift.X, Y = center.Y - shift.Y });
                    return true;
                }
                else if (dm.Content is CollectionView && (dm.Content as CollectionView)?.CurrentView is CollectionFreeformView)
                {
                    if (NavigateToDocument(root, rootViewModel ?? dm, (dm.Content as CollectionView)?.CurrentView as CollectionFreeformView, document))
                        return true;
                }
            return false;

        }
        public bool NavigateToDocument(CollectionFreeformView root, DocumentViewModel rootViewModel, CollectionFreeformView collection, DocumentController document)
        {
            if (collection?.ViewModel?.DocumentViewModels == null)
            {
                return false;
            }
            foreach (var dm in collection.ViewModel.DocumentViewModels)
                if (dm.DocumentController.Equals(document))
                {
                    var containerViewModel = rootViewModel ?? dm;
                    var canvas = root.xItemsControl.ItemsPanelRoot as Canvas;
                    var center = new Point((xMainDocView.ActualWidth - xMainTreeView.ActualWidth) / 2, xMainDocView.ActualHeight / 2);
                    var shift = canvas.TransformToVisual(xMainDocView).TransformPoint(
                        new Point(
                            containerViewModel.GroupTransform.Translate.X + containerViewModel.ActualWidth / 2,
                            containerViewModel.GroupTransform.Translate.Y + containerViewModel.ActualHeight / 2));
                    root.Move(new TranslateTransform() { X = center.X - shift.X, Y = center.Y - shift.Y });
                    return true;
                }
                else if (dm.Content is CollectionView && (dm.Content as CollectionView)?.CurrentView is CollectionFreeformView)
                {
                    if (NavigateToDocument(root, rootViewModel ?? dm, (dm.Content as CollectionView)?.CurrentView as CollectionFreeformView, document))
                        return true;
                }
            return false;
        }

        private void CoreWindowOnKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (xCanvas.Children.Contains(TabMenu.Instance))
            {
                TabMenu.Instance.HandleKeyDown(sender, e);
            }
        }

        private void CoreWindowOnKeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (e.Handled)
                return;
            if (e.VirtualKey == VirtualKey.Tab && !RichTextView.HasFocus)
            {
                var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                var x = pointerPosition.X - Window.Current.Bounds.X;
                var y = pointerPosition.Y - Window.Current.Bounds.Y;
                var pos = new Point(x, y);
                var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(pos, this).OfType<ICollectionView>().FirstOrDefault();
                if (topCollection == null)
                {
                    return;
                }

                // add tabitemviewmodels that directs user to documentviews within the current collection 

                TabMenu.ConfigureAndShow(topCollection as CollectionFreeformView, pos, xCanvas);
                TabMenu.Instance?.AddGoToTabItems();
            }

            // TODO propogate the event to the tab menu
            if (xCanvas.Children.Contains(TabMenu.Instance))
            {
                TabMenu.Instance.HandleKeyUp(sender, e);
            }


        }

        private void MainDocView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType != PointerDeviceType.Touch) return;
            var pointerPosition = e.GetPosition(this);
            var pos = new Point(pointerPosition.X - 20, pointerPosition.Y - 20);
            var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(pos, this).OfType<ICollectionView>()
                .FirstOrDefault();
            if (topCollection == null)
            {
                return;
            }
            TabMenu.ConfigureAndShow(topCollection as CollectionFreeformView, pos, xCanvas, true);
            e.Handled = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            GlobalInkSettings.Hue = 200;
            GlobalInkSettings.Brightness = 30;
            GlobalInkSettings.Size = 4;
            GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Pen;
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pen;
            GlobalInkSettings.Opacity = 1;

            OperatorMenuFlyout = new Flyout
            {
                Content = TabMenu.Instance,

            };

            xMainTreeView.DataContext = new CollectionViewModel(new DocumentFieldReference(MainDocument.Id, KeyStore.GroupingKey));

            //// add TreeMenu
            //TreeNode TreeMenu = new TreeNode(_mainCollectionView.ViewModel.CollectionController,null);
            //TreeMenu.Width = 300;
            //TreeMenu.HorizontalAlignment = HorizontalAlignment.Left;
            //MyGrid.Children.Add(TreeMenu);

        }

        public CollectionView GetMainCollectionView()
        {
            return _mainCollectionView ?? (_mainCollectionView = xMainDocView.GetFirstDescendantOfType<CollectionView>());
        }

        public void AddOperatorsFilter(ICollectionView collection, DragEventArgs e)
        {
            TabMenu.ConfigureAndShow(collection as CollectionFreeformView, e.GetPosition(Instance), xCanvas);
        }

        public void AddGenericFilter(object o, DragEventArgs e)
        {
            if (!xCanvas.Children.Contains(GenericSearchView.Instance))
            {
                xCanvas.Children.Add(GenericSearchView.Instance);
                Point absPos = e.GetPosition(Instance);
                Canvas.SetLeft(GenericSearchView.Instance, absPos.X);
                Canvas.SetTop(GenericSearchView.Instance, absPos.Y);
            }
        }

        /// <summary>
        /// Used to set the top-level options menu. Generally, this is envoked when
        /// the selected document changes & the options needs to be updated.
        /// </summary>
        /// <param name="menu"></param>
        public void SetOptionsMenu(OverlayMenu menu)
        {
            //            menu.CreateAndRunInstantiationAnimation(true);
            //xMenuCanvas.Content = menu;
        }

        /// <summary>
        ///     Adds new documents to the MainView document. New documents are added as children of the Main document.
        /// </summary>
        /// <param name="docModel"></param>
        /// <param name="where"></param>
        public void DisplayDocument(DocumentController docModel, Point? where = null)
        {
            if (where != null)
            {
                docModel.GetPositionField().Data = (Point)where;
            }
            var children = MainDocument.GetDereferencedField(KeyStore.CollectionKey, null) as ListController<DocumentController>;
            DBTest.ResetCycleDetection();
            children?.Add(docModel);
            //DBTest.DBDoc.AddChild(docModel);
        }

        public void DisplayElement(FrameworkElement elementToDisplay, Point upperLeft, UIElement fromCoordinateSystem)
        {
            var dropPoint = Util.PointTransformFromVisual(upperLeft, fromCoordinateSystem, xCanvas);
            // make sure elementToDisplay is never cut from screen 
            if (dropPoint.X > (xCanvas.ActualWidth - elementToDisplay.Width))
            {
                dropPoint.X = xCanvas.ActualWidth - elementToDisplay.Width - 50;
            }
            if (dropPoint.Y > (xCanvas.ActualHeight - elementToDisplay.Height))
            {
                dropPoint.Y = xCanvas.ActualHeight - elementToDisplay.Height - 10;
            }
            xCanvas.Children.Add(elementToDisplay);
            Canvas.SetLeft(elementToDisplay, dropPoint.X);
            Canvas.SetTop(elementToDisplay, dropPoint.Y);
        }

        public void ThemeChange()
        {
            this.RequestedTheme = this.RequestedTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        }


        private void CollapseButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            xTreeMenuColumn.Width = Math.Abs(xTreeMenuColumn.Width.Value) < .0001 ? new GridLength(300) : new GridLength(0);
        }

        private void xSearchButton_Tapped(object sender, TappedRoutedEventArgs e)
        {

            if (xSearchBoxGrid.Visibility == Visibility.Visible)
            {
                xSearchBoxGrid.Visibility = Visibility.Collapsed;
                xShowHideSearchIcon.Text = "\uE721"; // magnifying glass in segoe
            }
            else
            {
                SearchVisible = true;
                xSearchBoxGrid.Visibility = Visibility.Visible;
                xShowHideSearchIcon.Text = "\uE8BB"; // close button in segoe
                xMainSearchBox.Focus(FocusState.Programmatic);
            }
        }
    }
}