using DashShared;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Dash.Popups;
using Color = Windows.UI.Color;
using Point = Windows.Foundation.Point;
using System.Web;
using Windows.UI.Input;
using Windows.UI.Xaml.Media.Imaging;
using MyToolkit.Multimedia;
using Windows.Storage.Pickers;
using static Dash.DocumentController;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    ///     Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom.
    /// </summary>
    public sealed partial class MainPage : Page, ILinkHandler
    {
        public static Windows.UI.Input.PointerPoint PointerCaptureHack;  // saves a PointerPoint to be used for switching from a UWP manipulation to a Windows Drag Drop

        public enum PresentationViewState
        {
            Expanded,
            Collapsed
        }


        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;
        public static MainPage Instance { get; private set; }

        public BrowserView WebContext => BrowserView.Current;
        public DocumentController MainDocument { get; private set; }

        public SplitManager MainSplitter => XMainSplitter;

        // relating to system wide selected items
        public DocumentView xMapDocumentView;

        public PresentationViewState CurrPresViewState
        {
            get => MainDocument.GetDataDocument().GetField<BoolController>(KeyStore.PresentationViewVisibleKey)?.Data ?? false ? PresentationViewState.Expanded : PresentationViewState.Collapsed;
            set
            {
                bool state = value == PresentationViewState.Expanded;
                MainDocument.GetDataDocument().SetField<BoolController>(KeyStore.PresentationViewVisibleKey, state, true);
            }
        }

        public static int GridSplitterThickness { get; } = 7;

        public SettingsView GetSettingsView => xSettingsView;

        public InkManager InkManager { get; set; }

        public DashPopup ActivePopup;
        public Grid SnapshotOverlay => xSnapshotOverlay;
        public Storyboard FadeIn => xFadeIn;
        public Storyboard FadeOut => xFadeOut;

        public static PointerRoutedEventArgs PointerRoutedArgsHack = null;
        public MainPage()
        {
            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;
            InitializeComponent();
            //new Test().Process();
            SelectionManager.SelectionChanged += SelectionManagerSelectionChanged;
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            //formattableTitleBar.ButtonBackgroundColor = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]).Color;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = false;
            AddHandler(PointerMovedEvent, new PointerEventHandler((s, e) => PointerRoutedArgsHack = e), true);

            SetUpToolTips();

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
            Window.Current.CoreWindow.KeyUp += CoreWindowOnKeyUp;
            Window.Current.CoreWindow.KeyDown += CoreWindowOnKeyDown;

            Window.Current.CoreWindow.SizeChanged += (s, e) =>
            {
                double newHeight = e.Size.Height;
                double newWidth = e.Size.Width;
                if (ActivePopup != null)
                {
                    ActivePopup.SetHorizontalOffset((newWidth / 2) - 200 - (xLeftGrid.ActualWidth / 2));
                    //ActivePopup.SetVerticalOffset((newHeight / 2) - 150);
                    ActivePopup.SetVerticalOffset(200);
                }
            };

            xToolbar.SetValue(Canvas.ZIndexProperty, 20);

            SplitFrame.ActiveDocumentChanged += frame =>
            {
                MainDocument.GetDataDocument().SetField(KeyStore.LastWorkspaceKey, frame.DocumentController, true);
            };

            JavaScriptHack.ScriptNotify += JavaScriptHack_ScriptNotify;
            JavaScriptHack.NavigationCompleted += JavaScriptHack_NavigationCompleted;
        }

        public void Query(string search)
        {
            JavaScriptHack.Navigate(new Uri("https://www.google.com/search?q=" + search.Replace(' ', '+')));
        }

        private void JavaScriptHack_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            JavaScriptHack.InvokeScriptAsync("eval", new[] { "{ let elements = document.getElementsByClassName(\"Z0LcW\"); window.external.notify( elements.length > 0 ? elements[0].innerText : \"\"); }" });
        }

        private void JavaScriptHack_ScriptNotify(object sender, NotifyEventArgs e)
        {
            var value = e.Value as string;
            Debug.WriteLine("val = " + value);
        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await DotNetRPC.Init();

            var docs = await RESTClient.Instance.Fields.GetDocumentsByQuery<DocumentModel>(
                new DocumentTypeLinqQuery(DashConstants.TypeStore.MainDocumentType));
            var doc = docs.FirstOrDefault();
            if (doc != null)
            {
                MainDocument = RESTClient.Instance.Fields.GetController<DocumentController>(doc.Id);
            }
            else
            {
                MainDocument = new CollectionNote(new Point(), CollectionViewType.Freeform).Document;
                MainDocument.DocumentType = DashConstants.TypeStore.MainDocumentType;
                MainDocument.GetDataDocument().SetField<TextController>(KeyStore.TitleKey, "Workspaces", true);
            }
            FieldControllerBase.MakeRoot(MainDocument);

            var l = new ListController<TextController>();
            for (int i = 0; i < 2000; i++)
            {
                l.Add(new TextController());
            }

            MainDocument.SetField(KeyController.Get("some string it doesnt matter"), l, true);

            LoadSettings();

            //get current presentations if any and set data context of pres view to pres view model
            var presentations = MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.PresentationItemsKey, null);
            xPresentationView.DataContext = presentations != null ? new PresentationViewModel(presentations) : new PresentationViewModel();

            var col = MainDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            DocumentController lastWorkspace;
            if (col.Count == 0)
            {
                var documentController = new CollectionNote(new Point(), CollectionViewType.Freeform, double.NaN, double.NaN).Document;
                col.Add(documentController);
                lastWorkspace = documentController;
            }
            else
            {
                lastWorkspace = MainDocument.GetDataDocument().GetField<DocumentController>(KeyStore.LastWorkspaceKey);
            }

            XMainSplitter.SetContent(lastWorkspace);
            
            var treeContext = new CollectionViewModel(MainDocument.GetViewCopy(), KeyStore.DataKey);
            xMainTreeView.DataContext = treeContext;
            xMainTreeView.SetUseActiveFrame(true);
            //xMainTreeView.ToggleDarkMode(true);

            SetupMapView(lastWorkspace);

            if (CurrPresViewState == PresentationViewState.Expanded) SetPresentationState(true);
            InkManager = new InkManager();

            //OperatorScriptParser.TEST();
            //MultiLineOperatorScriptParser.TEST();
            TypescriptToOperatorParser.TEST();

            //this next line is optional and can be removed.  
            //Its only use right now is to tell the user that there is successful communication (or not) between Dash and the Browser
            //BrowserView.Current.SetUrl("https://en.wikipedia.org/wiki/Special:Random");


            // string localfolder = ApplicationData.Current.LocalFolder.Path;
            // var array = localfolder.Split('\\');
            // var username = array[2];
            // StorageFolder downloads = await StorageFolder.GetFolderFromPathAsync(@"C:\Users\" + username + @"\Downloads");
            // //replace byes (8).pdf with uploaded file name
            // StorageFile file = await downloads.GetFileAsync("byes (8).pdf");
            // FileData fileD = FileDropHelper.GetFileData(file, null).Result;
            // PdfToDashUtil PdftoDash = new PdfToDashUtil();
            //DocumentController docC = await PdftoDash.ParseFileAsync(fileD);
            // var mainPageCollectionView =
            //               MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
            // mainPageCollectionView.ViewModel.AddDocument(docC);
        }

        #region LOAD AND UPDATE SETTINGS

        private void LoadSettings()
        {
            var settingsDoc = GetAppropriateSettingsDoc();
            xSettingsView.LoadSettings(settingsDoc);
            XDocumentDecorations.LoadTags(settingsDoc);
        }

        private DocumentController GetAppropriateSettingsDoc()
        {
            var settingsDoc = MainDocument.GetDataDocument().GetField<DocumentController>(KeyStore.SettingsDocKey);
            if (settingsDoc != null) return settingsDoc;
            Debug.WriteLine("GETTING DEFAULT");
            settingsDoc = GetDefaultSettingsDoc();
            MainDocument.GetDataDocument().SetField(KeyStore.SettingsDocKey, settingsDoc, true);
            return settingsDoc;
        }

        private static DocumentController GetDefaultSettingsDoc()
        {
            var settingsDoc = new DocumentController();

            settingsDoc.SetField<BoolController>(KeyStore.SettingsNightModeKey, DashConstants.DefaultNightModeEngaged, true);
            settingsDoc.SetField<BoolController>(KeyStore.SettingsUpwardPanningKey, DashConstants.DefaultInfiniteUpwardPanningStatus, true);
            settingsDoc.SetField<NumberController>(KeyStore.SettingsFontSizeKey, DashConstants.DefaultFontSize, true);
            settingsDoc.SetField<TextController>(KeyStore.SettingsMouseFuncKey, SettingsView.MouseFuncMode.Zoom.ToString(), true);
            settingsDoc.SetField<TextController>(KeyStore.SettingsWebpageLayoutKey, SettingsView.WebpageLayoutMode.Default.ToString(), true);
            settingsDoc.SetField<NumberController>(KeyStore.SettingsNumBackupsKey, DashConstants.DefaultNumBackups, true);
            settingsDoc.SetField<NumberController>(KeyStore.SettingsBackupIntervalKey, DashConstants.DefaultBackupInterval, true);
            settingsDoc.SetField<TextController>(KeyStore.BackgroundImageStateKey, SettingsView.BackgroundImageState.Grid.ToString(), true);
            settingsDoc.SetField<NumberController>(KeyStore.BackgroundImageOpacityKey, 1.0, true);
            settingsDoc.SetField<BoolController>(KeyStore.SettingsMarkdownModeKey, false, true);
            settingsDoc.SetField<TextController>(KeyStore.AuthorKey, "New User", true);

            return settingsDoc;
        }

        #endregion

        private void CoreWindowOnKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (e.Handled || xMainSearchBox.GetDescendants().Contains(FocusManager.GetFocusedElement()))
                return;

            if (!(FocusManager.GetFocusedElement() is RichEditBox || FocusManager.GetFocusedElement() is TextBox || FocusManager.GetFocusedElement() is Dash.Views.TreeView.TreeViewNode))
            {
                if (this.IsCtrlPressed())
                {
                    if (e.VirtualKey == VirtualKey.Z)
                    {
                        UndoManager.UndoOccured();
                    }
                    else if (e.VirtualKey == VirtualKey.Y)
                    {
                        UndoManager.RedoOccured();
                    }
                }
            }

            if (xTabCanvas.Children.Contains(TabMenu.Instance))
            {
                TabMenu.Instance.HandleKeyDown(sender, e);
            }

            if (this.IsCtrlPressed() && e.VirtualKey.Equals(VirtualKey.F))
            {
                xSearchBoxGrid.Visibility = Visibility.Visible;
                xShowHideSearchIcon.Text = "\uE8BB"; // close button in segoe
                xMainSearchBox.Focus(FocusState.Programmatic);
            }

            if (DocumentView.FocusedDocument != null && !e.Handled)
            {
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
            }

            if (e.VirtualKey == VirtualKey.Back || e.VirtualKey == VirtualKey.Delete)
            {
                if (!(FocusManager.GetFocusedElement() is TextBox || FocusManager.GetFocusedElement() is RichEditBox || FocusManager.GetFocusedElement() is MarkdownTextBlock))
                {
                    using (UndoManager.GetBatchHandle())
                        foreach (var doc in SelectionManager.GetSelectedDocs())
                        {
                            doc.DeleteDocument();
                        }
                }
            }

            //deactivate all docs if esc was pressed
            if (e.VirtualKey == VirtualKey.Escape)
            {
                using (UndoManager.GetBatchHandle())
                {
                    LinkActivationManager.DeactivateAll();
                }

            }

            
       

            //activateall selected docs
            if (e.VirtualKey == VirtualKey.A && this.IsCtrlPressed())
            {
               
                var docs = SplitFrame.ActiveFrame.Document.GetImmediateDescendantsOfType<DocumentView>();
                SelectionManager.SelectDocuments(docs, this.IsShiftPressed());
            }
            
            e.Handled = true;
        }

        public void CollapseSearch()
        {
            xSearchBoxGrid.Visibility = Visibility.Collapsed;
            xShowHideSearchIcon.Text = "\uE721"; //magnifying glass in segoe
        }

        private void CoreWindowOnKeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (e.Handled || xMainSearchBox.GetDescendants().Contains(FocusManager.GetFocusedElement()))
            {
                if (xSearchBoxGrid.Visibility == Visibility.Visible && e.VirtualKey == VirtualKey.Escape)
                {
                    CollapseSearch();
                }
                return;
            }
            if (e.VirtualKey == VirtualKey.Tab && !(FocusManager.GetFocusedElement() is RichEditBox) &&
                !(FocusManager.GetFocusedElement() is TextBox))
            {
                var pos = this.RootPointerPos();
                var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(pos, this).OfType<CollectionView>().ToList();
                if (topCollection.FirstOrDefault()?.CurrentView is CollectionFreeformBase freeformView)
                {
                    TabMenu.ConfigureAndShow(freeformView, new Point(pos.X - xTreeMenuColumn.ActualWidth, pos.Y), xTabCanvas, true);
                    TabMenu.Instance?.AddGoToTabItems();
                }
            }

            // TODO propagate the event to the tab menu
            if (xTabCanvas.Children.Contains(TabMenu.Instance))
            {
                TabMenu.Instance.HandleKeyUp(sender, e);
            }

            if (e.VirtualKey == VirtualKey.Escape)
            {
                this.GetFirstDescendantOfType<CollectionView>().Focus(FocusState.Programmatic);
            }

            e.Handled = true;
        }

        public void ThemeChange(bool nightModeOn)
        {
            RequestedTheme = nightModeOn ? ElementTheme.Dark : ElementTheme.Light;
            xToolbar.SwitchTheme(nightModeOn);
        }

        private void xSearchButton_Tapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {

            if (xSearchBoxGrid.Visibility == Visibility.Visible)
            {
                xFadeAnimationOut.Begin();
                xSearchBoxGrid.Visibility = Visibility.Collapsed;
                xShowHideSearchIcon.Text = "\uE721"; // magnifying glass in segoe
            }
            else
            {
                xSearchBoxGrid.Visibility = Visibility.Visible;
                xFadeAnimationIn.Begin();
                xShowHideSearchIcon.Text = "\uE8BB"; // close button in segoe
                xMainSearchBox.Focus(FocusState.Programmatic);
            }
        }

        DispatcherTimer mapTimer = new DispatcherTimer();
        Button _mapActivateBtn = new Button() { Content = "^:" };
        public void SetupMapView(DocumentController mainDocumentCollection)
        {
            if (xMapDocumentView == null)
            {
                var xMap = RESTClient.Instance.Fields.GetController<DocumentController>("3D6910FE-54B0-496A-87E5-BE33FF5BB59C") ?? new CollectionNote(new Point(), CollectionViewType.Freeform).Document;
                xMap.SetFitToParent(true);
                xMap.SetWidth(double.NaN);
                xMap.SetHeight(double.NaN);
                xMap.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                xMap.SetVerticalAlignment(VerticalAlignment.Stretch);
                xMapDocumentView = new DocumentView() { DataContext = new DocumentViewModel(xMap) { Undecorated = true } };
                var overlay = new Grid();
                overlay.Background = new SolidColorBrush(Color.FromArgb(0x70, 0xff, 0xff, 0xff));

                _mapActivateBtn.HorizontalAlignment = HorizontalAlignment.Left;
                _mapActivateBtn.VerticalAlignment = VerticalAlignment.Top;
                _mapActivateBtn.Click += (s, e) => overlay.Background = overlay.Background == null ? new SolidColorBrush(Color.FromArgb(0x70, 0xff, 0xff, 0xff)) : null;
                overlay.Children.Add(_mapActivateBtn);

                Grid.SetColumn(overlay, 2);
                Grid.SetRow(overlay, 0);
                Grid.SetColumn(xMapDocumentView, 2);
                Grid.SetRow(xMapDocumentView, 0);
                xLeftStack.Children.Add(xMapDocumentView);
                xLeftStack.Children.Add(overlay);
                mapTimer.Interval = new TimeSpan(0, 0, 1);
                mapTimer.Tick += (ss, ee) => (xMapDocumentView.ViewModel.Content as CollectionView)?.FitContents();
                overlay.AddHandler(TappedEvent, new TappedEventHandler(XMapDocumentView_Tapped), true);
            } 

            xMapDocumentView.ViewModel.LayoutDocument.SetField(KeyStore.DocumentContextKey, mainDocumentCollection.GetDataDocument(), true);
            xMapDocumentView.ViewModel.LayoutDocument.SetField(KeyStore.DataKey, new DocumentReferenceController(mainDocumentCollection.GetDataDocument(), KeyStore.DataKey), true);
            mapTimer.Start();
        }

        private void XMapDocumentView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_mapActivateBtn.GetDescendants().Contains(e.OriginalSource))
                return;
            this.JavaScriptHack.Focus(FocusState.Programmatic);
            var mapViewCanvas = xMapDocumentView.GetFirstDescendantOfType<CollectionFreeformView>()?.GetItemsControl().GetFirstDescendantOfType<Canvas>();
            var mapPt = e.GetPosition(mapViewCanvas);

            var mainFreeform = SplitFrame.ActiveFrame.GetFirstDescendantOfType<CollectionFreeformView>();
            var mainFreeFormCanvas = mainFreeform?.GetItemsControl().GetFirstDescendantOfType<Canvas>();
            var mainFreeformXf = ((mainFreeFormCanvas?.RenderTransform ?? new MatrixTransform()) as MatrixTransform)?.Matrix ?? new Matrix();
            var mainDocCenter = new Point(SplitFrame.ActiveFrame.ActualWidth / 2 / mainFreeformXf.M11, SplitFrame.ActiveFrame.ActualHeight / 2 / mainFreeformXf.M22);
            var mainScale = new Point(mainFreeformXf.M11, mainFreeformXf.M22);
            mainFreeform?.SetTransformAnimated(
                new TranslateTransform() { X = -mapPt.X + SplitFrame.ActiveFrame.ActualWidth / 2, Y = -mapPt.Y + SplitFrame.ActiveFrame.ActualHeight / 2 },
                new ScaleTransform { CenterX = mapPt.X, CenterY = mapPt.Y, ScaleX = mainScale.X, ScaleY = mainScale.Y });
        }

        private void xSettingsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleSettingsVisibility(xSettingsView.Visibility == Visibility.Collapsed);
        }

        public void ToggleSettingsVisibility(bool changeToVisible)
        {
            xSettingsView.Visibility = changeToVisible ? Visibility.Visible : Visibility.Collapsed;
            //Toolbar.Visibility = changeToVisible ? Visibility.Collapsed : Visibility.Visible;
            xToolbar.ChangeVisibility(!changeToVisible);
        }

        //private void xSettingsButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        //{
        //    xSettingsButton.Fill = new SolidColorBrush(Colors.Gray);
        //}

        //private void xSettingsButton_PointerExited(object sender, PointerRoutedEventArgs e)
        //{
        //    xSettingsButton.Fill = (SolidColorBrush)App.Instance.Resources["AccentGreen"];
        //}

        public void SetPresentationState(bool expand, bool animate = true)
        {
            //    TogglePresentationMode(expand);

            if (expand)
            {
                CurrPresViewState = PresentationViewState.Expanded;
                if (animate)
                {
                    xPresentationExpand.Begin();
                    xPresentationExpand.Completed += (sender, o) =>
                    {
                        xPresentationView.xContentIn.Begin();
                        xPresentationView.xHelpIn.Begin();
                    };
                    xPresentationView.xContentIn.Completed += (sender, o) => { xPresentationView.xSettingsIn.Begin(); };
                    xPresentationView.xSettingsIn.Completed += (sender, o) =>
                    {
                        var isChecked = xPresentationView.xShowLinesButton.IsChecked;
                        if (isChecked ?? false) xPresentationView.ShowLines();
                    };
                }
                else
                {
                    xUtilTabColumn.MinWidth = 300;
                    xPresentationView.xTransportControls.Height = 60;
                    xPresentationView.SimulateAnimation(true);
                }

            }
            else
            {
                CurrPresViewState = PresentationViewState.Collapsed;
                //open presentation
                if (animate)
                {
                    xPresentationView.TryPlayStopClick();
                    xPresentationView.xSettingsOut.Begin();
                    xPresentationView.xContentOut.Begin();
                    xPresentationView.xHelpOut.Begin();
                    xPresentationRetract.Begin();
                }
                else
                {
                    xUtilTabColumn.MinWidth = 0;
                    xPresentationView.xTransportControls.Height = 0;
                    xPresentationView.SimulateAnimation(false);
                }

                PresentationView presView = Instance.xPresentationView;
                presView.xShowLinesButton.Background = new SolidColorBrush(Colors.White);
                presView.RemoveLines();
            }
        }

        public void PinToPresentation(DocumentController dc)
        {
            xPresentationView.ViewModel.AddToPinnedNodesCollection(dc);
            if (CurrPresViewState == PresentationViewState.Collapsed)
            {
                TextBlock help = xPresentationView.xHelpPrompt;
                help.Opacity = 0;
                help.Visibility = Visibility.Collapsed;
                SetPresentationState(true);
            }
            xPresentationView.DrawLinesWithNewDocs();
        }


        public async Task<PushpinType> GetPushpinType()
        {
            var typePopup = new PushpinTypePopup();
            SetUpPopup(typePopup);

            var mode = await typePopup.GetPushpinType();
            UnsetPopup();

            return mode;
        }

        public async Task<SettingsView.WebpageLayoutMode> GetLayoutType()
        {
            var importPopup = new HTMLRTFPopup();
            SetUpPopup(importPopup);

            var mode = await importPopup.GetLayoutMode();
            UnsetPopup();

            return mode;
        }

        public async Task<DocumentController> GetVideoFile()
        {
            var videoPopup = new ImportVideoPopup();
            SetUpPopup(videoPopup);

            var video = await videoPopup.GetVideoFile();
            UnsetPopup();// we may get a URL or a storage file -- I had a hard time with getting a StorageFile from a URI, so unfortunately right now they're separated

            if (video != null)
                switch (video.Type)
                {
                case VideoType.StorageFile:
                    return await new VideoToDashUtil().ParseFileAsync(video.File);
                case VideoType.Uri:
                    var query = HttpUtility.ParseQueryString(video.Uri.Query);
                    var videoId = query.AllKeys.Contains("v") ? query["v"] : video.Uri.Segments.Last();

                    try
                    {
                        var url = await YouTube.GetVideoUriAsync(videoId, YouTubeQuality.Quality1080P);
                        return VideoToDashUtil.CreateVideoBoxFromUri(url.Uri);
                    }
                    catch (Exception)
                    {
                        // TODO: display error video not found
                    }

                    break;
                }

            return null;
        }

        public async Task<DocumentController> GetImageFile()
        {
            var imagePopup = new ImportImagePopup();
            SetUpPopup(imagePopup);

            var image = await imagePopup.GetImageFile();
            UnsetPopup();

            return image != null ? await new ImageToDashUtil().ParseFileAsync(image) : null;
        }

        /// <summary>
        /// This method is always called right after a new popup is instantiated, and right before it's displayed, to set up its configurations.
        /// </summary>
        /// <param name="popup"></param>
        private void SetUpPopup(DashPopup popup)
        {
            ActivePopup = popup;
            xOverlay.Visibility = Visibility.Visible;
            popup.SetHorizontalOffset(((Frame)Window.Current.Content).ActualWidth / 2 - 200 - (xLeftGrid.ActualWidth / 2));
            popup.SetVerticalOffset(((Frame)Window.Current.Content).ActualHeight / 2 - 150);
            Grid.SetColumn(popup.Self(), 2);
            xOuterGrid.Children.Add(popup.Self());
        }

        /// <summary>
        /// This method is called after a popup closes, to remove it from the page.
        /// </summary>
        private void UnsetPopup()
        {
            xOverlay.Visibility = Visibility.Collapsed;
            if (ActivePopup != null)
            {
                xOuterGrid.Children.Remove(ActivePopup.Self());
                ActivePopup = null;
            }
        }

        public void NavigateToDocument(DocumentController doc)//More options
        {
            var tree = DocumentTree.MainPageTree;
            var node = tree.FirstOrDefault(n => n.ViewDocument.Equals(doc));
            if (node?.Parent == null)
            {
                SplitFrame.OpenInActiveFrame(doc);
                return;
            }

            SplitFrame.OpenDocumentInWorkspace(doc, node.Parent.ViewDocument);
        }

        public void NavigateToDocumentOrRegion(DocumentController docOrRegion, DocumentController link = null)//More options
        {
            DocumentController parent = docOrRegion.GetRegionDefinition();
            (parent ?? docOrRegion).SetHidden(false);
            NavigateToDocument(parent ?? docOrRegion);
            if (parent != null)
            {
                parent.GotoRegion(docOrRegion, link);
            }
        }

        public void ToggleFloatingDoc(DocumentController doc)
        {
            var onScreenView = GetTargetDocumentView(doc);

            if (onScreenView != null)
            {
                var highlighted = onScreenView.ViewModel.SearchHighlightState != DocumentViewModel.UnHighlighted;
                onScreenView.ViewModel.SetHighlight(true);
                if (highlighted)
                {
                    onScreenView.ViewModel.LayoutDocument.ToggleHidden();
                }
            }
            else
            {
                var floaty = xCanvas.Children.OfType<Grid>().FirstOrDefault(g => g.Children.FirstOrDefault() is DocumentView dv && dv.ViewModel.DataDocument.Equals(doc.GetDataDocument()));
                if (floaty != null)
                {
                    xCanvas.Children.Remove(floaty);
                }
                else
                {
                    AddFloatingDoc(doc, null, new Point(xCanvas.PointerPos().X + 25, xCanvas.PointerPos().Y));
                }
            }
        }


        private void SelectionManagerSelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            if (args.SelectedViews.Count > 0)
            {
                if (xCanvas.Children.OfType<Grid>().Any(g => g.Children.FirstOrDefault() is DocumentView dv && SelectionManager.GetSelectedDocs().Contains(dv)))
                    return;
            }

            MainPage.Instance.GetDescendantsOfType<DocumentView>().Where((dv) => dv.ViewModel?.IsHighlighted ?? false).ToList().ForEach((dv) => dv.ViewModel?.SetHighlight(false));
            ClearFloaty(null);
        }


        public void AddFloatingDoc(DocumentController doc, Point? size = null, Point? position = null)
        {
            var onScreenView = GetTargetDocumentView(doc);
            if (onScreenView != null)
            {
                return;
            }

            //make doc view out of doc controller
            var docCopy = doc.GetViewCopy();
            if (doc.DocumentType.Equals(CollectionBox.DocumentType))
            {
                docCopy.SetWidth(400);
                docCopy.SetHeight(300);
                docCopy.SetFitToParent(true);
            }
            var origWidth = doc.GetWidth();
            var origHeight = doc.GetHeight();
            var aspect = !double.IsNaN(origWidth) && origWidth != 0 && !double.IsNaN(origHeight) && origHeight != 0 ? origWidth / origHeight : 1;
            docCopy.SetWidth(size?.X ?? 150);
            docCopy.SetHeight(size?.Y ?? 150 / aspect);
            docCopy.SetBackgroundColor(Colors.White);
            //put popup slightly left of center, so its not covered centered doc
            var defaultPt = position ?? new Point(xCanvas.ActualWidth / 2 - 250, xCanvas.ActualHeight / 2 - 50);

            var docView = new DocumentView
            {
                DataContext = new DocumentViewModel(docCopy),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                BindRenderTransform = false
            };

            var grid = new Grid
            {
                RenderTransform = new TranslateTransform() { X = defaultPt.X, Y = defaultPt.Y }
            };
            grid.Children.Add(docView);
            var btn = new Button() { Content = "X" };
            btn.Width = btn.Height = 20;
            btn.Background = new SolidColorBrush(Colors.Red);
            btn.HorizontalAlignment = HorizontalAlignment.Left;
            btn.VerticalAlignment = VerticalAlignment.Top;
            btn.Margin = new Thickness(0, -10, -10, 10);
            btn.Click += (s, e) => xCanvas.Children.Remove(grid);
            grid.Children.Add(btn);

            xCanvas.Children.Add(grid);
        }

        public void ClearFloaty(DocumentView dragged)
        {
            xCanvas.Children.OfType<Grid>().Where((g) => g.Children.FirstOrDefault() is DocumentView dv && (dv == dragged || dragged == null)).ToList().ForEach((g) =>
                 xCanvas.Children.Remove(g));
        }

        #region Annotation logic

        public LinkHandledResult HandleLink(DocumentController linkDoc, LinkDirection direction)
        {
            var region = linkDoc.GetDataDocument().GetLinkedDocument(direction);
            var target = region.GetRegionDefinition() ?? region;

            if (this.IsCtrlPressed() && !this.IsAltPressed())
            {
                NavigateToDocumentOrRegion(region, linkDoc);
                return LinkHandledResult.HandledClose;
            }
            var onScreenView = GetTargetDocumentView(target);

            if (target.GetLinkBehavior() == LinkBehavior.Overlay)
            {
                target.GotoRegion(region, linkDoc);
                onScreenView?.ViewModel.SetHighlight(true);
                return LinkHandledResult.HandledRemainOpen;
            }

            if (onScreenView != null) // we found the hyperlink target being displayed somewhere *onscreen*.  If it's hidden, show it.  If it's shown in the main workspace, hide it. If it's show in a docked pane, remove the docked pane.
            {
                var highlighted = onScreenView.ViewModel.SearchHighlightState != DocumentViewModel.UnHighlighted;
                onScreenView.ViewModel.SetHighlight(true);
                if (highlighted && (target.Equals(region) || target.GetField<DocumentController>(KeyStore.GoToRegionKey)?.Equals(region) == true)) // if the target is a document or a visible region ...
                {
                    //    if (onScreenView.GetFirstAncestorOfType<DockedView>() == xMainDocView.GetFirstDescendantOfType<DockedView>()) // if the document was on the main screen (either visible or hidden), we toggle it's visibility
                    onScreenView.ViewModel.LayoutDocument.ToggleHidden();
                    //    else DockManager.Undock(onScreenView.GetFirstAncestorOfType<DockedView>()); // otherwise, it was in a docked pane -- instead of toggling the target's visibility, we just removed the docked pane.
                }
                else // otherwise, it's a hidden region that we have to show
                {
                    onScreenView.ViewModel.LayoutDocument.SetHidden(false);
                }
            }
            else
            {
                //Dock_Link(linkDoc, direction);
                //target.SetHidden(false);
                ToggleFloatingDoc(target);
            }

            target.GotoRegion(region, linkDoc);

            return LinkHandledResult.HandledRemainOpen;
        }

        public void DockLink(DocumentController linkDoc, LinkDirection direction, bool inContext = true)
        {
            var region = linkDoc.GetDataDocument().GetLinkedDocument(direction);
            var target = region.GetRegionDefinition() ?? region;
            var frame = MainSplitter.GetFrameWithDoc(target, true);
            if (frame != null)
            {
                frame.Delete();
            }
            else
            {
                //TODO Splitting: Deal with inContext
                SplitFrame.OpenInInactiveFrame(target);
            }
        }

        public DocumentView GetTargetDocumentView(DocumentController target)
        {
            //TODO Do this search the other way around, only checking documents in view instead of checking all documents and then seeing if it is in view
            var dataDoc = target.GetDataDocument();
            var docViews = MainSplitter.GetDescendantsOfType<DocumentView>().Where(v => v.ViewModel != null && v.ViewModel.DataDocument.Equals(dataDoc)).ToList();
            if (!docViews.Any())
            {
                return null;
            }

            DocumentView found = null;

            foreach (var view in docViews)
            {
                found = view;
                foreach (var parentView in view.GetAncestorsOfType<DocumentView>())
                {
                    var transformedBounds = view.TransformToVisual(parentView)
                        .TransformBounds(new Rect(0, 0, view.ActualWidth, view.ActualHeight));
                    var parentBounds = new Rect(0, 0, parentView.ActualWidth, parentView.ActualHeight);
                    bool containsTL = parentBounds.Contains(new Point(transformedBounds.Left, transformedBounds.Top));
                    bool containsBR = parentBounds.Contains(new Point(transformedBounds.Right, transformedBounds.Bottom));
                    bool containsTR = parentBounds.Contains(new Point(transformedBounds.Right, transformedBounds.Top));
                    bool containsBL = parentBounds.Contains(new Point(transformedBounds.Left, transformedBounds.Bottom));
                    if (!(containsTL || containsBR || containsBL || containsTR))
                    {
                        found = null;
                    }
                }
                if (found != null)
                    return found;
            }

            return null;
        }

        #endregion

        public void Timeline_OnCompleted(object sender, object e)
        {
            xSnapshotOverlay.Visibility = Visibility.Collapsed;
        }

        private void XOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
        }

        private void XOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
        }

        private void XLoadingPopup_OnClosed(object sender, object e)
        {
            xOverlay.Visibility = Visibility.Collapsed;
        }

        private void XLoadingPopup_OnOpened(object sender, object e)
        {
            xOverlay.Visibility = Visibility.Visible;
        }

        private void SetUpToolTips()
        {
            const PlacementMode placementMode = PlacementMode.Bottom;
            const int offset = 5;

            var search = new ToolTip()
            {
                Content = "Search workspace",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xSearchButton, search);
        }

        public async Task<(string, string)> PromptNewTemplate()
        {
            var templatePopup = new NewTemplatePopup();
            SetUpPopup(templatePopup);

            var results = await templatePopup.GetFormResults();
            UnsetPopup();

            return results;
        }

        public async Task<(KeyController, List<KeyController>)> PromptJoinTables(List<KeyController> comparisonKeys, List<KeyController> diffKeys, List<KeyController> draggedKeys)
        {
            var tablePopup = new JoinGroupMenuPopup(comparisonKeys, diffKeys, draggedKeys);
            SetUpPopup(tablePopup);

            var results = await tablePopup.GetFormResults();
            UnsetPopup();

            return results;
        }
    }


}
