using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DashShared;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Dash.Views.Document_Menu;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Visibility = Windows.UI.Xaml.Visibility;
using System.Timers;
using Dash.Views;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    ///     Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Instance { get; private set; }
        
        public BrowserView          WebContext => BrowserView.Current;
        public DocumentController   MainDocument { get; private set; }
        public DocumentView         MainDocView { get { return xMainDocView; } set { xMainDocView = value; } }

        public DocumentView         xMapDocumentView;

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

            var radialMenu = new RadialMenuView(xCanvas);
            radialMenu.Loaded += (s,e) => radialMenu.JumpToPosition(3 * ActualWidth / 4, 3 * ActualHeight / 4);

            Loaded += (s, e) =>
            {
                GlobalInkSettings.Hue = 200;
                GlobalInkSettings.Brightness = 30;
                GlobalInkSettings.Size = 4;
                GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Pen;
                GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pen;
                GlobalInkSettings.Opacity = 1;
                xMainDocView.ViewModel.DisableDecorations = true;

                xMainTreeView.DataContext = new CollectionViewModel(new DocumentFieldReference(MainDocument.Id, KeyStore.DataKey));
            };

            xSplitter.Tapped += (s,e) => xTreeMenuColumn.Width = Math.Abs(xTreeMenuColumn.Width.Value) < .0001 ? new GridLength(300) : new GridLength(0);
            xBackButton.Tapped += (s, e) => GoBack();
            Window.Current.CoreWindow.KeyUp += CoreWindowOnKeyUp;
            Window.Current.CoreWindow.KeyDown += CoreWindowOnKeyDown;
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
                        var layout = new CollectionBox(
                                new DocumentReferenceController(MainDocument.GetId(), KeyStore.DataKey)).Document;
                        MainDocument.SetActiveLayout(layout, true, true);
                    }
                }
                else
                {
                    var fields = new Dictionary<KeyController, FieldControllerBase>
                    {
                        [KeyStore.DataKey] = new ListController<DocumentController>(),
                    };
                    MainDocument = new DocumentController(fields, DashConstants.TypeStore.MainDocumentType);
                    var layout = new CollectionBox(new DocumentReferenceController(MainDocument.GetId(), KeyStore.DataKey)).Document;
                    MainDocument.SetActiveLayout(layout, true, true);
                }

                var col = MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.DataKey);
                var history =
                    MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceHistoryKey);
                DocumentController lastWorkspace;
                if (col.Count == 0)
                {
                    var documentController = new NoteDocuments.CollectionNote(new Point(0, 0),
                        CollectionView.CollectionViewType.Freeform).Document;
                    col.Add(documentController);
                    MainDocument.SetField(KeyStore.LastWorkspaceKey, documentController, true);
                    lastWorkspace = documentController;
                }
                else
                {
                    lastWorkspace = MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
                }
                lastWorkspace.SetWidth(double.NaN);
                lastWorkspace.SetHeight(double.NaN);

                MainDocView.DataContext = new DocumentViewModel(lastWorkspace);

                setupMapView(lastWorkspace);
            }

            await RESTClient.Instance.Fields.GetDocumentsByQuery<DocumentModel>(
                new DocumentTypeLinqQuery(DashConstants.TypeStore.MainDocumentType), Success, ex => throw ex);

            OperatorScriptParser.TEST();
            MultiLineOperatorScriptParser.TEST();

            BrowserView.ForceInit();

            //this next line is optional and can be removed.  
            //Its only use right now is to tell the user that there is successful communication (or not) between Dash and the Browser
            //BrowserView.Current.SetUrl("https://en.wikipedia.org/wiki/Special:Random");
        }

        public bool SetCurrentWorkspace(DocumentController workspace)
        {
            //prevents us from trying to enter the main document.  Can remove this for further extensibility but it doesn't work yet
            if (workspace.Equals(MainDocument))
            {
                return false;
            }
            var currentWorkspace = MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
            if (currentWorkspace.Equals(workspace))
            {
                return true;
            }
            var workspaceViewCopy = workspace;
            if (workspaceViewCopy.GetDereferencedField<TextController>(KeyStore.CollectionFitToParentKey, null)?.Data == "true") //  !isWorkspace)
            {
                workspaceViewCopy = workspace.GetViewCopy();
                workspaceViewCopy.SetField<NumberController>(KeyStore.WidthFieldKey, double.NaN, true);
                workspaceViewCopy.SetField<NumberController>(KeyStore.HeightFieldKey, double.NaN, true);
                workspaceViewCopy.SetField<TextController>(KeyStore.CollectionFitToParentKey, "false", true);
            }
            MainDocView.DataContext = new DocumentViewModel(workspaceViewCopy);
            setupMapView(workspaceViewCopy);
            MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceHistoryKey).Add(currentWorkspace);
            MainDocument.SetField(KeyStore.LastWorkspaceKey, workspaceViewCopy, true);
            return true;
        }

        public void GoBack()
        {
            var history =
                MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceHistoryKey);
            if (history.Count != 0)
            {
                var workspace = history.TypedData.Last();
                history.Remove(workspace);
                MainDocView.DataContext = new DocumentViewModel(workspace);
                setupMapView(workspace);
                MainDocument.SetField(KeyStore.LastWorkspaceKey, workspace, true);
            }
        }

        public void SetCurrentWorkspaceAndNavigateToDocument(DocumentController workspace, DocumentController document)
        {
            RoutedEventHandler handler = null;
            handler =
                delegate(object sender, RoutedEventArgs args)
                {
                    MainDocView.xContentPresenter.Loaded -= handler;


                    var dvm = MainDocView.DataContext as DocumentViewModel;
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
            MainDocView.xContentPresenter.Loaded += handler;
            if (!SetCurrentWorkspace(workspace))
            {
                MainDocView.xContentPresenter.Loaded -= handler;
            }
        }

        public bool NavigateToDocumentInWorkspace(DocumentController document)
        {
            var dvm = MainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            if (coll != null)
            {
                return NavigateToDocument(coll, null, coll, document);
            }
            return false;
        }

        public void HighlightTreeView(DocumentController document, bool ?flag)
        {
            xMainTreeView.Highlight(document, flag);
        }

        public void HighlightDoc(DocumentController document, bool ?flag)
        {
            var dvm = MainDocView.DataContext as DocumentViewModel;
            var collection = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            if (collection != null && document != null)
            {
                highlightDoc(collection, document, flag);
            }
        }

        private void highlightDoc(CollectionFreeformView collection, DocumentController document, bool ?flag)
        {
            foreach (var dm in collection.ViewModel.DocumentViewModels)
                if (dm.DocumentController.Equals(document))
                {
                    if (flag == null)
                        dm.DecorationState = (dm.Undecorated == false) && !dm.DecorationState;
                    else if (flag == true)
                        dm.DecorationState = (dm.Undecorated == false);
                    else if (flag == false)
                        dm.DecorationState = false;
                }
                else if (dm.Content is CollectionView && (dm.Content as CollectionView)?.CurrentView is CollectionFreeformView freeformView)
                {
                    highlightDoc(freeformView, document, flag);
                }
        }

        public bool NavigateToDocumentInWorkspaceAnimated(DocumentController document)
        {
            var dvm = MainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            if (coll != null && document !=  null)
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
                    var center = new Point((MainDocView.ActualWidth - xMainTreeView.ActualWidth) / 2, MainDocView.ActualHeight / 2);
                    var shift = canvas.TransformToVisual(MainDocView).TransformPoint(
                        new Point(
                            containerViewModel.XPos + containerViewModel.ActualSize.X / 2,
                            containerViewModel.YPos + containerViewModel.ActualSize.Y / 2));

                    

                    var pt = canvas.TransformToVisual(MainDocView).TransformPoint(new Point(0, 0));
                    var oldTranslateX = (canvas.RenderTransform as MatrixTransform).Matrix.OffsetX;
                    var oldTranslateY = (canvas.RenderTransform as MatrixTransform).Matrix.OffsetY;

                    root.MoveAnimated(new TranslateTransform() { X = center.X - shift.X, Y = center.Y - shift.Y });
                    return true;
                }
                else if (dm.Content is CollectionView && (dm.Content as CollectionView)?.CurrentView is CollectionFreeformView)
                {
                    if (NavigateToDocumentAnimated(root, rootViewModel ?? dm, (dm.Content as CollectionView)?.CurrentView as CollectionFreeformView, document))
                        return true;
                }
            return false;

        }
        public bool NavigateToDocument(CollectionFreeformView root, DocumentViewModel rootViewModel, CollectionFreeformView collection, DocumentController document)
        {
            if (collection?.ViewModel?.DocumentViewModels == null || !root.IsInVisualTree())
            {
                return false;
            }
            foreach (var dm in collection.ViewModel.DocumentViewModels)
                if (dm.DocumentController.Equals(document))
                {
                    var containerViewModel = rootViewModel ?? dm;
                    var canvas = root.xItemsControl.ItemsPanelRoot as Canvas;
                    var center = new Point((MainDocView.ActualWidth - xMainTreeView.ActualWidth) / 2, MainDocView.ActualHeight / 2);
                    var shift = canvas.TransformToVisual(MainDocView).TransformPoint(
                        new Point(
                            containerViewModel.XPos + containerViewModel.ActualSize.X / 2,
                            containerViewModel.YPos + containerViewModel.ActualSize.Y / 2));
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
            if (e.VirtualKey == VirtualKey.Tab && !(FocusManager.GetFocusedElement() is RichEditBox))
            {
                MainDocView_OnDoubleTapped(null, null);
            }

            // TODO propagate the event to the tab menu
            if (xCanvas.Children.Contains(TabMenu.Instance))
            {
                TabMenu.Instance.HandleKeyUp(sender, e);
            }

            if (e.VirtualKey == VirtualKey.Back || e.VirtualKey == VirtualKey.Delete)
            {
                var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(this.RootPointerPos(), this).OfType<CollectionView>().ToList();
                foreach (var c in topCollection.Select((c) => c.CurrentView).OfType<CollectionFreeformView>())
                    if (c.SelectedDocs.Count() > 0)
                    {
                        foreach (var d in c.SelectedDocs)
                            d.DeleteDocument();
                        break;
                    }
            }
        }

        private void MainDocView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var pos = this.RootPointerPos();
            var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(pos,this).OfType<CollectionView>().ToList();
            if (topCollection.FirstOrDefault()?.CurrentView is CollectionFreeformView freeformView)
            {
                if (e != null)
                {
                    foreach (var d in freeformView.xItemsControl.ItemsPanelRoot.Children)
                    {
                        if (d is ContentPresenter presenter)
                        {
                            if (presenter.Content is DocumentViewModel dvm)
                            {
                                if (dvm.DocumentController.DocumentType.Equals(BackgroundBox.DocumentType))
                                {
                                    var dv = d.GetFirstDescendantOfType<DocumentView>();
                                    var hit = dv.IsHitTestVisible;
                                    dv.IsHitTestVisible = true;
                                    var hits = VisualTreeHelper.FindElementsInHostCoordinates(pos, dv).ToList();
                                    e.Handled = hits.Count > 0;
                                    dv.IsHitTestVisible = hits.Count > 0 ? !hit : hit;
                                    if (!dv.IsHitTestVisible)
                                        dvm.DecorationState = dv.IsHitTestVisible;
                                    if (e.Handled)
                                        break;
                                }
                            }
                        }
                    }
                }

                if (e == null || !e.Handled)
                {
                    TabMenu.ConfigureAndShow(freeformView, pos, xCanvas, true);
                    TabMenu.Instance?.AddGoToTabItems();
                    if (e != null)
                        e.Handled = true;
                }
            }
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
        
        public void ThemeChange()
        {
            this.RequestedTheme = this.RequestedTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
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
                xSearchBoxGrid.Visibility = Visibility.Visible;
                xShowHideSearchIcon.Text = "\uE8BB"; // close button in segoe
                xMainSearchBox.Focus(FocusState.Programmatic);
            }
        }

        DispatcherTimer mapTimer = new DispatcherTimer();
        void setupMapView(DocumentController mainDocumentCollection)
        {
            if (xMapDocumentView == null)
            {
                var xMap = ContentController<FieldModel>.GetController<DocumentController>("3D6910FE-54B0-496A-87E5-BE33FF5BB59C") ?? new NoteDocuments.CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform).Document;
                xMap.SetField<TextController>(KeyStore.CollectionFitToParentKey, "true", true);
                xMap.SetField<NumberController>(KeyStore.WidthFieldKey, double.NaN, true);
                xMap.SetField<NumberController>(KeyStore.HeightFieldKey, double.NaN, true);
                xMapDocumentView = new DocumentView() { DataContext = new DocumentViewModel(xMap), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                Grid.SetColumn(xMapDocumentView, 2);
                xLeftStack.Children.Add(xMapDocumentView);
                mapTimer.Interval = new TimeSpan(0, 0, 1);
                mapTimer.Tick += (ss, ee) => xMapDocumentView.GetFirstDescendantOfType<CollectionView>()?.ViewModel?.FitContents();
            }
            xMapDocumentView.ViewModel.LayoutDocument.SetField(KeyStore.DocumentContextKey, mainDocumentCollection.GetDataDocument(), true);
            mapTimer.Start();
        }

        private void snapshotButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (MainDocView.GetFirstDescendantOfType<CollectionFreeformView>() is CollectionFreeformView freeFormView)
                xMainTreeView.ViewModel.ContainerDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.Add(freeFormView.Snapshot());
        }

        public void Dock(DocumentView toDock)
        {
            ColumnDefinition newSplitterDefinition = new ColumnDefinition();
            ColumnDefinition newDockDefinition = new ColumnDefinition();
            newSplitterDefinition.Width = new GridLength(15, GridUnitType.Pixel);
            newDockDefinition.Width = new GridLength(1, GridUnitType.Star);
            xOuterGrid.ColumnDefinitions.Add(newSplitterDefinition);
            xOuterGrid.ColumnDefinitions.Add(newDockDefinition);

            DocumentController context = toDock.ViewModel.DocumentController;
            DocumentView copiedView = new DocumentView()
            {
                DataContext = new DocumentViewModel(context.GetViewCopy()),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            copiedView.ViewModel.Width = Double.NaN;
            copiedView.ViewModel.Height = Double.NaN;
            copiedView.ViewModel.DisableDecorations = true;

            GridSplitter splitter = new GridSplitter();
            DockedView dockedView = new DockedView(splitter);
            dockedView.ChangeView(copiedView);

            Grid.SetColumn(splitter, xOuterGrid.ColumnDefinitions.Count - 2); //second-to-last
            Grid.SetColumn(dockedView, xOuterGrid.ColumnDefinitions.Count - 1);

            xOuterGrid.Children.Add(splitter);
            xOuterGrid.Children.Add(dockedView);
        }

        public void HighlightDock()
        {
            xDock.Opacity = 0.4;
        }

        public void UnhighlightDock()
        {
            xDock.Opacity = 0;
        }

        public void Undock(DockedView undock)
        {
            xOuterGrid.Children.Remove(undock);
            xOuterGrid.Children.Remove(undock.Splitter);
        }
    }
}