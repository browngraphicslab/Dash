using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
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
using Dash.Views.Document_Menu;
using Dash.Controllers;
using Windows.UI.Popups;
using Windows.Foundation.Collections;
using System.IO;
using System.Security.Permissions;
using Windows.Storage;
using Windows.ApplicationModel.DataTransfer;


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
        
        // relating to system wide selected items
        public DocumentView xMapDocumentView;
        private  ICollection<DocumentView> SelectedDocuments; // currently selected documents

        private bool IsPresentationModeToggled = false;

        private bool[] _firstDock = {true, true, true, true};
        private DockedView[] _lastDockedViews = {null, null, null, null};

        public static int GridSplitterThickness { get; } = 7;

        // TODO: change this to Toolbar binding to SelectedDocuments
        public void DeselectAllDocuments()
        {
            SelectedDocuments = new List<DocumentView>();
            Toolbar.Update(SelectedDocuments);
        }
        public void DeselectDocument(DocumentView doc)
        {
            if (SelectedDocuments?.Count() > 0)
            {
                SelectedDocuments.Remove(doc);
                Toolbar.Update(SelectedDocuments);
            }
        }
        public void SelectDocument(DocumentView doc) => SelectDocuments( new List<DocumentView>() { doc } );
        public void SelectDocuments(ICollection<DocumentView> docs)
        {
            SelectedDocuments = docs;
            Toolbar.Update(docs);
        }

    public IEnumerable<DocumentView> GetSelectedDocuments() => SelectedDocuments;

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

            Loaded += (s, e) =>
            {
                GlobalInkSettings.Hue = 200;
                GlobalInkSettings.Brightness = 30;
                GlobalInkSettings.Size = 4;
                GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Pen;
                GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pen;
                GlobalInkSettings.Opacity = 1;
            };

            xSplitter.Tapped += (s, e) => xTreeMenuColumn.Width = Math.Abs(xTreeMenuColumn.Width.Value) < .0001 ? new GridLength(300) : new GridLength(0);
            xBackButton.Tapped += (s, e) => GoBack();
            Window.Current.CoreWindow.KeyUp += CoreWindowOnKeyUp;
            Window.Current.CoreWindow.KeyDown += CoreWindowOnKeyDown;

			Toolbar.SetValue(Canvas.ZIndexProperty, 20);

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
                    var documentController = new CollectionNote(new Point(0, 0),
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

                MainDocView.ViewModel = new DocumentViewModel(lastWorkspace) {DisableDecorations = true};

                var treeContext = new CollectionViewModel(MainDocument, KeyStore.DataKey);
                //TODO This might not be necessary and shouldn't be necessary
                treeContext.Loaded(true);
                xMainTreeView.DataContext = treeContext;
                xMainTreeView.ChangeTreeViewTitle("My Workspaces");
                xMainTreeView.ToggleDarkMode(true);

                setupMapView(lastWorkspace);
            }

            await DotNetRPC.Init();

            await RESTClient.Instance.Fields.GetDocumentsByQuery<DocumentModel>(
                new DocumentTypeLinqQuery(DashConstants.TypeStore.MainDocumentType), Success, ex => throw ex);

            //OperatorScriptParser.TEST();
            //MultiLineOperatorScriptParser.TEST();
            TypescriptToOperatorParser.TEST();

            BrowserView.ForceInit();

            //this next line is optional and can be removed.  
            //Its only use right now is to tell the user that there is successful communication (or not) between Dash and the Browser
            //BrowserView.Current.SetUrl("https://en.wikipedia.org/wiki/Special:Random");


            //FileSystemWatcher watcher = new FileSystemWatcher();
            //watcher.Path = "C:\\Users\\GFX lab\\Downloads";
            ///* Watch for changes in LastAccess and LastWrite times, and
            //   the renaming of files or directories. */
            //watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
            //   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            //// Only watch text files.
            //watcher.Filter = "*.pdf";

            //Subscribe to the Created event.
            //watcher.Created += new FileSystemEventHandler(watcher_FileCreated);

            ////Enable the FileSystemWatcher events.
            //watcher.EnableRaisingEvents = true;

            //TODO: UWP apps can't access Downloads
            StorageFolder downloads = await StorageFolder.GetFolderFromPathAsync("C:\\Users\\GFX lab\\Downloads");
            StorageFile file = await downloads.GetFileAsync("hello.pdf");
            IStorageFile Ifile = file;
            //DataPackageView dataPack = file;
            FileData fileD = FileDropHelper.GetFileData(file, null).Result;
           // DocumentController docPdf = PdfToDashUtil.ParseFileAsync(fileD);

        }

        private static void watcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        }

        /// <summary>
        /// Updates the workspace currently displayed on the canvas.
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns></returns>
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
            var workspaceViewCopy = workspace.GetViewCopy();
            workspaceViewCopy.SetField<NumberController>(KeyStore.WidthFieldKey, double.NaN, true);
            workspaceViewCopy.SetField<NumberController>(KeyStore.HeightFieldKey, double.NaN, true);
            if (workspaceViewCopy.GetDereferencedField<TextController>(KeyStore.CollectionFitToParentKey, null)?.Data == "true") //  !isWorkspace)
            {
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

        /// <summary>
        /// Given a Workspace document (collection freeform), displays the workspace on the main canvas
        /// and centers on a specific document.
        /// </summary>
        /// <param name="workspace"></param>
        /// <param name="document"></param>
        public void SetCurrentWorkspaceAndNavigateToDocument(DocumentController workspace, DocumentController document)
        {
            RoutedEventHandler handler = null;
            handler =
                delegate (object sender, RoutedEventArgs args)
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
                                finalHandler = delegate (object finalSender, RoutedEventArgs finalArgs)
                                {
                                    Debug.WriteLine("loaded");
                                    NavigateToDocumentInWorkspace(document, false);
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
                                contentHandler = delegate (object contentSender, RoutedEventArgs contentArgs)
                                {
                                    dvm.Content.Loaded -= contentHandler;
                                    if (!NavigateToDocumentInWorkspace(document, false))
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
                                    if (!NavigateToDocumentInWorkspace(document, false))
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

        /// <summary>
        /// Centers the main canvas view to a given document.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public bool NavigateToDocumentInWorkspace(DocumentController document, bool animated, bool compareDataDocuments=false)
        {
            var dvm = MainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            if (coll != null)
            {
                return NavigateToDocument(coll, null, coll, document, animated, compareDataDocuments);
            }
            return false;
        }

        public void HighlightTreeView(DocumentController document, bool? flag)
        {
            xMainTreeView.Highlight(document, flag);
        }

        public void HighlightDoc(DocumentController document, bool? flag, int search=0)
        {
            var dvm = MainDocView.DataContext as DocumentViewModel;
            var collection = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            if (collection != null && document != null)
            {
                highlightDoc(collection, document, flag, search);
            }
        }

        private void highlightDoc(CollectionFreeformView collection, DocumentController document, bool? flag, int search)
        {
            foreach (var dm in collection.ViewModel.DocumentViewModels)
                if (dm.DocumentController.Equals(document))
                {
                    //for search - 0 means no change, 1 means turn highlight on, 2 means turn highlight off
                    if (search == 0)
                    {
                        if (flag == null)
                            dm.DecorationState = (dm.Undecorated == false) && !dm.DecorationState;
                        else if (flag == true)
                            dm.DecorationState = (dm.Undecorated == false);
                        else if (flag == false)
                            dm.DecorationState = false;
                    }
                    else if(search == 1)
                    {
                        //highlight doc
                        dm.SearchHighlightState = new Thickness(8);
                    }
                    else
                    {
                        //unhighlight doc
                        dm.SearchHighlightState = new Thickness(0);
                    }
                }
                else if (dm.Content is CollectionView && (dm.Content as CollectionView)?.CurrentView is CollectionFreeformView freeformView)
                {
                    highlightDoc(freeformView, document, flag, search);
                }
        }

        public bool NavigateToDocumentInWorkspaceAnimated(DocumentController document)
        {
            var dvm = MainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            if (coll != null && document != null)
            {
                return NavigateToDocument(coll, null, coll, document, true, true);
            }
            return false;
        }

        public bool NavigateToDocument(CollectionFreeformView root, DocumentViewModel rootViewModel, CollectionFreeformView collection, DocumentController document, bool animated, bool compareDataDocuments=false)
        {
            if (collection?.ViewModel?.DocumentViewModels == null || !root.IsInVisualTree())
            {
                return false;
            }
            foreach (var dm in collection.ViewModel.DocumentViewModels)
            {
                var dmd = dm.DocumentController.GetDataDocument();
                var dd = document.GetDataDocument();
                if (dm.DocumentController.Equals(document) || (compareDataDocuments && dm.DocumentController.GetDataDocument().Equals(document.GetDataDocument())))
                {
                    var containerViewModel = rootViewModel ?? dm;
                    var canvas = root.xItemsControl.ItemsPanelRoot as Canvas;
                    var center = new Point((MainDocView.ActualWidth - xMainTreeView.ActualWidth) / 2, MainDocView.ActualHeight / 2);
                    var shift = canvas.TransformToVisual(MainDocView).TransformPoint(
                        new Point(
                            containerViewModel.XPos + containerViewModel.ActualSize.X / 2,
                            containerViewModel.YPos + containerViewModel.ActualSize.Y / 2));
                    if (animated)
                        root.MoveAnimated(new TranslateTransform() { X = center.X - shift.X, Y = center.Y - shift.Y });
                    else root.Move(new TranslateTransform() { X = center.X - shift.X, Y = center.Y - shift.Y });
                    return true;
                }
                else if (dm.Content is CollectionView && (dm.Content as CollectionView)?.CurrentView is CollectionFreeformView)
                {
                    if (NavigateToDocument(root, rootViewModel ?? dm, (dm.Content as CollectionView)?.CurrentView as CollectionFreeformView, document, animated, compareDataDocuments))
                        return true;
                }
            }
            return false;
        }

        private void CoreWindowOnKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            Debug.WriteLine("FOCUSED = " + FocusManager.GetFocusedElement());
            if (xCanvas.Children.Contains(TabMenu.Instance))
            {
                TabMenu.Instance.HandleKeyDown(sender, e);
            }

            if (DocumentView.FocusedDocument != null && !e.Handled) {
                if (this.IsShiftPressed() && !e.VirtualKey.Equals(VirtualKey.Shift))
                {
                    if (DocumentView.FocusedDocument.ViewModel != null && e.VirtualKey.Equals(VirtualKey.Enter)) // shift + Enter
                    {
                        // don't shift enter on KeyValue documents (since they already display the key/value adding)
                        if (!DocumentView.FocusedDocument.ViewModel.LayoutDocument.DocumentType.Equals(KeyValueDocumentBox.DocumentType) &&
                            !DocumentView.FocusedDocument.ViewModel.DocumentController.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType))
                            DocumentView.FocusedDocument.HandleShiftEnter();
                    }
                }
                if (this.IsF1Pressed() && this.IsPointerOver())
                {
                    DocumentView.FocusedDocument.ShowLocalContext(true);
                }
                if (this.IsF2Pressed() && this.IsPointerOver())
                {
                    DocumentView.FocusedDocument.ShowSelectedContext();
                }
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

            if (e.VirtualKey == VirtualKey.Escape)
            {
                this.GetFirstDescendantOfType<CollectionView>().Focus(FocusState.Programmatic);
                e.Handled = true;
            }

            if (e.VirtualKey == VirtualKey.Back || e.VirtualKey == VirtualKey.Delete)
            {
               if (FocusManager.GetFocusedElement() is TextBox)
                    return;
                var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(this.RootPointerPos(), this).OfType<CollectionView>().ToList();
                foreach (var c in topCollection.Select((c) => c.CurrentView).OfType<CollectionFreeformView>())
                    if (c.SelectedDocs.Count() > 0)
                    {
                        foreach (var d in c.SelectedDocs)
                            d.DeleteDocument();
                        break;
                    }
            }

            var dvm = MainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            
            // TODO: this should really only trigger when the marquee is inactive -- currently it doesn't happen fast enough to register as inactive, and this method fires
            if (!coll.IsMarqueeActive())
            {
                coll.TriggerActionFromSelection(e.VirtualKey, false);
            }
            
            if (DocumentView.FocusedDocument != null)
            {
                if (!this.IsF1Pressed())
                    DocumentView.FocusedDocument.ShowLocalContext(false);
            }
        }

        private void MainDocView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var pos = this.RootPointerPos();
            var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(pos, this).OfType<CollectionView>().ToList();
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
                                if (dvm.IsAdornmentGroup)
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

        public void ThemeChange(bool nightModeOn)
        {
            RequestedTheme = nightModeOn ? ElementTheme.Dark : ElementTheme.Light; 
			Toolbar.SwitchTheme(nightModeOn);
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
                var xMap = ContentController<FieldModel>.GetController<DocumentController>("3D6910FE-54B0-496A-87E5-BE33FF5BB59C") ?? new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform).Document;
                xMap.SetField<TextController>(KeyStore.CollectionFitToParentKey, "true", true);
                xMap.SetField<NumberController>(KeyStore.WidthFieldKey, double.NaN, true);
                xMap.SetField<NumberController>(KeyStore.HeightFieldKey, double.NaN, true);
                xMapDocumentView = new DocumentView() { DataContext = new DocumentViewModel(xMap), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                //xMapDocumentView.IsHitTestVisible = false;
                Grid.SetColumn(xMapDocumentView, 2);
                Grid.SetRow(xMapDocumentView, 0);
                xLeftStack.Children.Add(xMapDocumentView);
                mapTimer.Interval = new TimeSpan(0, 0, 1);
                mapTimer.Tick += (ss, ee) => xMapDocumentView.GetFirstDescendantOfType<CollectionView>()?.ViewModel?.FitContents();
            }
            xMapDocumentView.ViewModel.LayoutDocument.SetField(KeyStore.DocumentContextKey, mainDocumentCollection.GetDataDocument(), true);
            xMapDocumentView.ViewModel.LayoutDocument.SetField(KeyStore.DataKey, new DocumentReferenceController(mainDocumentCollection.GetDataDocument().Id, KeyStore.DataKey), true);
            mapTimer.Start();
        }

        private void snapshotButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (MainDocView.GetFirstDescendantOfType<CollectionFreeformView>() is CollectionFreeformView freeFormView)
                xMainTreeView.ViewModel.ContainerDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.Add(freeFormView.Snapshot());
        }

        public void Dock(DocumentView toDock, DockDirection dir)
        {
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

            DockedView dockedView = new DockedView(dir);
            dockedView.ChangeView(copiedView);
            dockedView.HorizontalAlignment = HorizontalAlignment.Stretch;
            dockedView.VerticalAlignment = VerticalAlignment.Stretch;

            if (_firstDock[(int) dir])
            {
                switch (dir)
                {
                    case DockDirection.Left:
                        xLeftDockSplitterColumn.Width = new GridLength(GridSplitterThickness);
                        xLeftDockColumn.Width = new GridLength(300);
                        SetGridPosition(dockedView, 2, 1, 0, 5);
                        break;
                    case DockDirection.Right:
                        xRightDockSplitterColumn.Width = new GridLength(GridSplitterThickness);
                        xRightDockColumn.Width = new GridLength(300);
                        SetGridPosition(dockedView, 6, 1, 0, 5);
                        break;
                    case DockDirection.Top:
                        xTopDockSplitterRow.Height = new GridLength(GridSplitterThickness);
                        xTopDockRow.Height = new GridLength(200);
                        SetGridPosition(dockedView, 4, 1, 0, 1);
                        break;
                    case DockDirection.Bottom:
                        xBottomDockSplitterRow.Height = new GridLength(GridSplitterThickness);
                        xBottomDockRow.Height = new GridLength(200);
                        SetGridPosition(dockedView, 4, 1, 4, 1);
                        break;
                }
                
                xOuterGrid.Children.Add(dockedView);
                _firstDock[(int) dir] = false;
                _lastDockedViews[(int) dir] = dockedView;
            }
            else
            {
                DockedView tail = _lastDockedViews[(int) dir];
                tail.ChangeNestedView(dockedView);
                dockedView.PreviousView = tail;
                _lastDockedViews[(int) dir] = dockedView;
            }
            
        }

        private void SetGridPosition(FrameworkElement e, int col, int colSpan, int row, int rowSpan)
        {
            Grid.SetColumn(e, col);
            Grid.SetColumnSpan(e, colSpan);
            Grid.SetRow(e, row);
            Grid.SetRowSpan(e, rowSpan);
        }

        public void HighlightDock(DockDirection dir)
        {
            switch (dir)
            {
                case DockDirection.Left:
                    xDockLeft.Opacity = 0.4;
                    break;
                case DockDirection.Right:
                    xDockRight.Opacity = 0.4;
                    break;
                case DockDirection.Top:
                    xDockTop.Opacity = 0.4;
                    break;
                case DockDirection.Bottom:
                    xDockBottom.Opacity = 0.4;
                    break;
            }
        }

        public void UnhighlightDock()
        {
            xDockRight.Opacity = 0;
            xDockLeft.Opacity = 0;
            xDockTop.Opacity = 0;
            xDockBottom.Opacity = 0;
        }

        public void Undock(DockedView undock)
        {
            // means it's the last NestedView
            if (undock.NestedView == null)
            {
                // means it's also the first NestedView
                if (undock.PreviousView == null)
                {
                    switch (undock.Direction)
                    {
                        case DockDirection.Left:
                            xLeftDockSplitterColumn.Width = new GridLength(0);
                            xLeftDockColumn.Width = new GridLength(0);
                            break;
                        case DockDirection.Right:
                            xRightDockSplitterColumn.Width = new GridLength(0);
                            xRightDockColumn.Width = new GridLength(0);
                            break;
                        case DockDirection.Top:
                            xTopDockSplitterRow.Height = new GridLength(0);
                            xTopDockRow.Height = new GridLength(0);
                            break;
                        case DockDirection.Bottom:
                            xBottomDockSplitterRow.Height = new GridLength(0);
                            xBottomDockRow.Height = new GridLength(0);
                            break;
                    }
                    xOuterGrid.Children.Remove(undock);
                    _firstDock[(int) undock.Direction] = true;
                    _lastDockedViews[(int) undock.Direction] = null;
                }
                else
                {
                    undock.PreviousView.ClearNestedView();
                    _lastDockedViews[(int) undock.Direction] = undock.PreviousView;
                }
            }
            else
            {
                // means it's the first NestedView
                if (undock.PreviousView == null)
                {
                    var newFirst = undock.ClearNestedView();
                    newFirst.PreviousView = null;
                    xOuterGrid.Children.Remove(undock);
                    switch (undock.Direction)
                    {
                        case DockDirection.Left:
                            SetGridPosition(newFirst, 2, 1, 0, 5);
                            break;
                        case DockDirection.Right:
                            SetGridPosition(newFirst, 6, 1, 0, 5);
                            break;
                        case DockDirection.Top:
                            SetGridPosition(newFirst, 4, 1, 0, 1);
                            break;
                        case DockDirection.Bottom:
                            SetGridPosition(newFirst, 4, 1, 4, 1);
                            break;
                    }
                    xOuterGrid.Children.Add(newFirst);
                }
                else
                {
                    var newNext = undock.ClearNestedView();
                    newNext.PreviousView = undock.PreviousView;
                    undock.PreviousView.ChangeNestedView(newNext);
                }
            }
        }
        
        private void xSettingsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleSettingsVisibility(xSettingsView.Visibility == Visibility.Collapsed);
        }

        public void ToggleSettingsVisibility(bool changeToVisible)
        {
            xSettingsView.Visibility = changeToVisible ? Visibility.Visible : Visibility.Collapsed;
            Toolbar.Visibility = changeToVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void xSettingsButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            xSettingsButton.Fill = new SolidColorBrush(Colors.Gray);
        }

        private void xSettingsButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            xSettingsButton.Fill = (SolidColorBrush)App.Instance.Resources["AccentGreen"];
        }

        public void TogglePresentationMode()
        {
            IsPresentationModeToggled = !IsPresentationModeToggled;
            xMainTreeView.TogglePresentationMode(IsPresentationModeToggled);
            xUtilTabColumn.Width = IsPresentationModeToggled ? new GridLength(330) : new GridLength(0);
        }

        public void PinToPresentation(DocumentViewModel viewModel)
        {
            xPresentationView.ViewModel.AddToPinnedNodesCollection(viewModel);
            if (!IsPresentationModeToggled)
                TogglePresentationMode();
        }

      
    }
}
