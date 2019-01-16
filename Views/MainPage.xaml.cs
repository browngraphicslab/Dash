using DashShared;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
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
using Dash.Converters;
using Dash.Popups.TemplatePopups;
using static Dash.DocumentController;
using Dash.Controllers.Functions.Operators;
using TemplateType = Dash.TemplateList.TemplateType;
using Windows.UI.Xaml.Data;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    ///     Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom.
    /// </summary>
    public sealed partial class MainPage : Page, ILinkHandler
    {
        private Timer _lowPriorityTimer      = new Timer(3600000);  // every hour
        private Timer _moderatePriorityTimer = new Timer(900000);   // every 15 minutes
        private Timer _highPriorityTimer     = new Timer(5000);     // every 15 seconds

        public static MainPage               Instance { get; private set; }
        public static PointerRoutedEventArgs PointerRoutedArgsHack = null;

        public SplitManager                       MainSplitter    => XMainSplitter;
        public Grid                               SnapshotOverlay => xSnapshotOverlay;
        public BrowserView                        WebContext      => BrowserView.Current;
        public SettingsView                       SettingsView    => xSettingsView;
        public Storyboard                         FadeIn          => xFadeIn;
        public Storyboard                         FadeOut         => xFadeOut;
        public DashPopup                          ActivePopup;
        public InkManager                         InkManager   { get; set; }
        public DocumentController                 MainDocument { get; private set; }
        public ListController<DocumentController> LowPriorityOps;
        public ListController<DocumentController> ModeratePriorityOps;
        public ListController<DocumentController> HighPriorityOps;

        public MainPage()
        {
            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;
            InitializeComponent();
            //new Test().Process();
            SelectionManager.SelectionChanged += SelectionManagerSelectionChanged;
            var formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonForegroundColor = Colors.White;
            //formattableTitleBar.ButtonBackgroundColor = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]).Color;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;

            AddHandler(PointerPressedEvent, new PointerEventHandler(HoverHighlight), true);
            AddHandler(PointerMovedEvent,   new PointerEventHandler(HoverHighlight), true);
            AddHandler(KeyUpEvent,          new KeyEventHandler(HoverHighlightKey), true);

            ToolTipService.SetToolTip(xSearchButton, new ToolTip() { Content = "Search workspace", Placement = PlacementMode.Bottom, VerticalOffset = 5 });

            Loaded += (s, e) =>
            {
                GlobalInkSettings.Hue = 200;
                GlobalInkSettings.Brightness = 30;
                GlobalInkSettings.Size = 4;
                GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Pen;
                GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pen;
                GlobalInkSettings.Opacity = 1;
                xToolbar.Visibility = Visibility.Collapsed;
            };

            xSplitter.Tapped += (s, e) => xTreeMenuColumn.Width = Math.Abs(xTreeMenuColumn.Width.Value) < .0001 ? new GridLength(300) : new GridLength(0);

            Window.Current.CoreWindow.KeyDown += CoreWindowOnKeyDown;
            Window.Current.CoreWindow.SizeChanged += (s, e) =>
            {
                if (ActivePopup != null)
                {
                    ActivePopup.SetHorizontalOffset((e.Size.Width / 2) - 200 - (xLeftGrid.ActualWidth / 2));
                    //ActivePopup.SetVerticalOffset((e.Size.Height / 2) - 150);
                    ActivePopup.SetVerticalOffset(200);
                }
            };

            Canvas.SetZIndex(xToolbar, 20);

            SplitFrame.ActiveDocumentChanged += frame => MainDocument.GetDataDocument().SetField(KeyStore.LastWorkspaceKey, frame.DocumentController, true);

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(trickyTitleBar);
        }

        public DocumentController MiscellaneousFolder
        {
            get
            {
                var folders = MainDocument.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.DataKey);
                var misc = folders.Where((doc) => doc.Title == "Miscellaneous").FirstOrDefault();
                if (misc == null)
                {
                    misc = new CollectionNote(new Point(), CollectionViewType.Freeform).Document;
                    misc.SetTitle("Miscellaneous");
                    MainDocument.GetDataDocument().AddToListField(KeyStore.DataKey, misc);
                    // folders.Add(misc);
                }
                return misc;
            }
        }

        public void SinkFocus() { this.GetFirstDescendantOfType<CollectionView>().Focus(FocusState.Programmatic); }
        public void OpenSearchBox()
        {
            xSearchBoxGrid.Visibility = Visibility.Visible;
            xMainSearchBox.Focus(FocusState.Programmatic);
        }
        public void SetOverlayVisibility(Visibility visibility) { xOverlay.Visibility = visibility; }
        public void SetSearchVisibility(Visibility visibility)  { xSearchBoxGrid.Visibility = visibility; }
        public void SetMapVisibility(Visibility visibility)     { xMapView.SetVisibility(visibility, xLeftGrid); }
        public void SetMapTarget(DocumentController layoutDocument) { xMapView.SetTarget(layoutDocument); }

        public void AddFloatingDoc(DocumentController doc, Point? size = null, Point? position = null)
        {
            var onScreenView = FindTargetDocumentView(doc);
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
            if (!doc.DocumentType.Equals(RichTextBox.DocumentType))
            {
                docCopy.SetWidth(size?.X ?? 150);
                docCopy.SetHeight(size?.Y ?? 150 / aspect);
            }
            docCopy.SetBackgroundColor(Colors.White);
            //put popup slightly left of center, so its not covered centered doc
            var defaultPt = position ?? new Point(xCanvas.ActualWidth / 2 - 250, xCanvas.ActualHeight / 2 - 50);

            FieldControllerBase.MakeRoot(docCopy); // when do I release this?
            var docView = new DocumentView
            {
                DataContext = new DocumentViewModel(docCopy),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                BindRenderTransform = false,
            };

            var grid = new Grid
            {
                Padding = new Thickness(5),
                Background = new SolidColorBrush(Color.FromArgb(0x20, 0x10, 0x10, 0x10)),
                CornerRadius = new CornerRadius(10),
                RenderTransform = new TranslateTransform() { X = defaultPt.X, Y = defaultPt.Y }
            };
            grid.Children.Add(docView);
            //var btn = new Button() { Content = "X" };
            //btn.Width = btn.Height = 20;
            //btn.Background = new SolidColorBrush(Colors.Red);
            //btn.HorizontalAlignment = HorizontalAlignment.Left;
            //btn.VerticalAlignment = VerticalAlignment.Top;
            //btn.Margin = new Thickness(0, -10, -10, 10);
            //btn.Click += (s, e) => xCanvas.Children.Remove(grid);
            //grid.Children.Add(btn);

            xCanvas.Children.Add(grid);
        }

        public void ClearFloatingDoc(DocumentView dragged)
        {
            xCanvas.Children.OfType<Grid>().Where(g => g.Children.FirstOrDefault() is DocumentView dv && (dv == dragged || dragged == null)).ToList().ForEach(g =>
                 xCanvas.Children.Remove(g));
        }
        public bool IsFloatingDoc(DocumentView docView)
        {
            return docView.GetAncestors().Contains(xCanvas);
        }
        public void MoveFloatingDoc(DocumentView dragged, Point where)
        {
            xCanvas.Children.OfType<Grid>().Where(g => g.Children.FirstOrDefault() is DocumentView dv && (dv == dragged || dragged == null)).ToList().
                ForEach(g => g.RenderTransform = new TranslateTransform() { X = where.X, Y = where.Y } );
        }


        public void       ThemeChange(bool nightModeOn) { RequestedTheme = nightModeOn ? ElementTheme.Dark : ElementTheme.Light; } //xToolbar.SwitchTheme(nightModeOn);
        public async void Publish()
        {
            // TODO: do the following eventually; for now it will just export everything you have
            // var documentList = await GetDocumentsToPublish();
            var allDocuments = DocumentTree.MainPageTree.Select(node => node.DataDocument).Distinct().Where(node => !node.DocumentType.Equals(CollectionNote.CollectionNoteDocumentType)).ToList();
            allDocuments.Remove(MainDocument.GetDataDocument());

            await new Publisher().StartPublication(allDocuments);
        }

        public async Task<string>                                  PromptLayoutTemplate(IEnumerable<DocumentController> docs)
        {
            var popup = new LayoutTemplatesPopup();
            SetUpPopup(popup);
            //TODO: Eventually unset this popup after templatePopup, so that user can exit back
            var templateType = await popup.GetTemplate();
            UnsetPopup();
            return templateType == TemplateType.None ? null : await processTemplate(docs, templateType);
        }
        public async Task<SettingsView.WebpageLayoutMode>          PromptLayoutType()
        {
            var importPopup = new HTMLRTFPopup();
            SetUpPopup(importPopup);
            var mode = await importPopup.GetLayoutMode();
            UnsetPopup();
            return mode;
        }
        public async Task<(string, string)>                        PromptNewTemplate()
        {
            var popup = new NewTemplatePopup();
            SetUpPopup(popup);
            var results = await popup.GetFormResults();
            UnsetPopup();
            return results;
        }
        public async Task<(List<DocumentController>,List<string>)> PromptTravelogue()
        {
            var traveloguePopup = new TraveloguePopup();
            SetUpPopup(traveloguePopup);
            var results = await traveloguePopup.GetFormResults();
            UnsetPopup();
            return results;
        }
        public async Task<(KeyController, List<KeyController>)>    PromptJoinTables(List<KeyController> comparisonKeys, List<KeyController> diffKeys, List<KeyController> draggedKeys)
        {
            var tablePopup = new JoinGroupMenuPopup(comparisonKeys, diffKeys, draggedKeys);
            SetUpPopup(tablePopup);
            var results = await tablePopup.GetFormResults();
            UnsetPopup();
            return results;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await DotNetRPC.Init();

            await DocumentScope.InitGlobalScope();

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

            if (RESTClient.Instance.Fields is CachedEndpoint ce)
            {
                await ce.Cleanup();
            }
            xSettingsView.LoadSettings(GetAppropriateSettingsDoc());

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

            var treeContext = new CollectionViewModel(MainDocument, KeyStore.DataKey);
            xMainTreeView.DataContext = treeContext;
            xMainTreeView.SetUseActiveFrame(true);
            //xMainTreeView.ToggleDarkMode(true);

            var toolbar = MainDocument.GetField<DocumentController>(KeyStore.ToolbarKey);
            if (toolbar == null)
            {
                toolbar = new CollectionNote(new Point(), CollectionViewType.Grid, double.NaN, 70).Document;
                await InitToolbar(toolbar);
                MainDocument.SetField(KeyStore.ToolbarKey, toolbar, true);
            }

            MenuToolbar.Instance.SetCollection(toolbar);

            if (xPresentationView.CurrPresViewState == PresentationView.PresentationViewState.Expanded)
            {
                xPresentationView.SetPresentationState(true);
            }

            InkManager = new InkManager();

            //OperatorScriptParser.TEST();
            //MultiLineOperatorScriptParser.TEST();
            TypescriptToOperatorParser.TEST();

            LowPriorityOps = MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.LowPriorityOpsKey);
            ModeratePriorityOps = MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.ModeratePriorityOpsKey);
            HighPriorityOps = MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.HighPriorityOpsKey);

            _lowPriorityTimer.Elapsed += async (sender, args) => await AgentTimerExecute(sender, args, LowPriorityOps);
            _moderatePriorityTimer.Elapsed += async (sender, args) => await AgentTimerExecute(sender, args, ModeratePriorityOps);
            _highPriorityTimer.Elapsed += async (sender, args) => await AgentTimerExecute(sender, args, HighPriorityOps);

            _lowPriorityTimer.Start();
            _moderatePriorityTimer.Start();
            _highPriorityTimer.Start();

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
            
            EventManager.LoadEvents(MainDocument.GetField<ListController<DocumentController>>(KeyStore.EventManagerKey));
            MenuToolbar.Instance.xStackPanel.Children.Remove(MenuToolbar.Instance.xSubtoolbarStackPanel);
            var subPanel = MenuToolbar.Instance.xSubtoolbarStackPanel;
            customTitleBar.Children.Add(subPanel);
            //subPanel.HorizontalAlignment = HorizontalAlignment.Center;
            //MenuToolbar.Instance.xSubtoolbarStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
            //MenuToolbar.Instance.xSubtoolbarStackPanel.Margin = new Thickness(100, 0, 0, 0);
        }
        
        private void SelectionManagerSelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            if (args.SelectedViews.Count == 0 ||
                !xCanvas.Children.OfType<Grid>().Any(g => g.Children.FirstOrDefault() is DocumentView dv && SelectionManager.SelectedDocViewModels.Contains(dv.ViewModel)))
            {
                Instance.GetDescendantsOfType<DocumentView>().Where((dv) => dv.ViewModel?.IsHighlighted ?? false).ToList().ForEach((dv) => dv.ViewModel?.SetSearchHighlightState(false));
                if (!args.SelectedViews.Any(dv => dv.GetFirstAncestorOfType<SplitFrame>() == null)) // clear the floating documents unless the newly selected document is a floating document
                {
                    ClearFloatingDoc(null);
                }
            }
        }

        private async Task<bool> AgentTimerExecute(object sender, ElapsedEventArgs e, ListController<DocumentController> opList)
        {
            if (opList.Any())
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var tasks = new List<Task>(opList.Count);
                    foreach (var opDoc in opList)
                    {
                        var op = opDoc.GetField<OperatorController>(KeyStore.ScheduledOpKey);
                        var layoutDoc = opDoc.GetField<DocumentController>(KeyStore.ScheduledDocKey);
                        var task = OperatorScript.Run(op, new List<FieldControllerBase>() {layoutDoc}, new DictionaryScope());
                        if (!task.IsFaulted) tasks.Add(task);
                        else Debug.WriteLine("TASK FAULTED!");
                    }

                    if (tasks.Any())
                    {
                        await Task.WhenAll(tasks);
                    }
                });

                return true;
            }

            return false;
        }

        #region LOAD TOOLBAR AND UPDATE SETTINGS
        private async Task                     InitToolbar(DocumentController toolbar)
        {
            toolbar.SetBackgroundColor(ColorConverter.HexToColor("#6DA8DE"));
            var data = toolbar.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);

            var buttons = new List<(string icon, string name, bool rotate, string function)>
            {
//                ("\uE923", "Make Instance", false, @"
//function (d) {
//    for(var doc in get_selected_docs()) {
//        if(doc.Parent == null) {
//            continue;
//        }
//        doc.Parent.Data.add(doc.Document.instance());
//    }
//}
//"),
//                ("\uF571", "Make View Copy", false, @"
//function (d) {
//    for(var doc in get_selected_docs()) {
//        if(doc.Parent == null) {
//            continue;
//        }
//        doc.Parent.Data.add(doc.Document.view_copy());
//    }
//}
//"),
//                ("\uE16F", "Make Copy", false, @"
//function (d) {
//    for(var doc in get_selected_docs()) {
//        if(doc.Parent == null) {
//            continue;
//        }
//        doc.Parent.Data.add(doc.Document.copy());
//    }
//}
//"),
//                ("\uE840", "Pin", false, @"
//function (d) {
//    for(var doc in get_selected_docs()) {
//        doc.Document.AreContentsHitTestVisible = false;
//    }
//}
//"),
//                ("\uE77A", "Unpin", false, @"
//function (d) {
//    for(var doc in get_selected_docs()) {
//        doc.Document.AreContentsHitTestVisible = true;
//    }
//}
//"),
                ("\uEC8F", "Fit Width", true, @"
function (d) {
    for(var doc in get_selected_docs()) {
        if(doc.Document.get_field(""Horizontal Alignment"") == ""Stretch"") {
            doc.Document.set_field(""Horizontal Alignment"", ""Left"");
            doc.Document.Width = doc.Document._StoredWidth;
            doc.Document._StoredWidth = null;
        } else {
            doc.Document.set_field(""Horizontal Alignment"", ""Stretch"");
            doc.Document._StoredWidth = doc.Document.Width;
            doc.Document.Width = NaN;
        }
    }
}
"),
                ("\uEC8F", "Fit Height", false, @"
function (d) {
    for(var doc in get_selected_docs()) {
        if(doc.Document.get_field(""Vertical Alignment"") == ""Stretch"") {
            doc.Document.set_field(""Vertical Alignment"", ""Top"");
            doc.Document.Height = doc.Document._StoredHeight;
            doc.Document._StoredHeight = null;
        } else {
            doc.Document.set_field(""Vertical Alignment"", ""Stretch"");
            doc.Document._StoredHeight = doc.Document.Height;
            doc.Document.Height = NaN;
        }
    }
}
"),
                ("\uE10E", "Undo", false, @"
function(d) {
    undo();
}
"),
                ("\uE10D", "Redo", false, @"
function(d) {
    redo();
}
"),
                ("\uF57C", "Split Horizontal", false, @"
function (d) {
    split_horizontal();
}
"),
                ("\uE985", "Split Vertical", false, @"
function (d) {
    split_vertical();
}
"),
                ("\uE8BB", "Close Split", false, @"
function (d) {
    close_split();
}
"),
//                ("\uE72B", "Back", false, @"
//function (d) {
//    frame_history_back();
//}
//"),
//                ("\uE72A", "Forward", false, @"
//function (d) {
//    frame_history_forward();
//}
//"),
//                ("\uE898", "Export", false, @"
//function (d) {
//    export_workspace();
//}
//"),
//                ("\uE768", "Toggle Presentation", false, @"
//function (d) {
//    toggle_presentation();
//}
//"),
            };

            await Task.WhenAll(buttons.Select(async item => data.Add(await GetButton(item.icon, item.function, item.name, item.rotate))));
        }
        private async Task<DocumentController> GetButton(string icon, string tappedHandler, string name, bool rotate)
        {
            var op = await new DSL().Run(tappedHandler, true) as OperatorController;
            if (op == null)
            {
                return null;
            }
            var doc = new DocumentController();
            doc.SetXaml(
                @"
<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
      xmlns:dash='using:Dash'
      xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006'>
    <TextBlock x:Name='xTextFieldData' FontSize='32' FontFamily='Segoe MDL2 Assets' Foreground='White' TextAlignment='Center'>
" + (rotate ? @"
        <TextBlock.RenderTransform>
            <RotateTransform Angle=""90"" CenterX=""16"" CenterY=""16"" />
        </TextBlock.RenderTransform>
" : "") + @"
    </TextBlock>
</Grid>");
            doc.SetField<TextController>(KeyStore.DataKey, icon, true);
            doc.SetTitle(name);
            doc.SetField<TextController>(KeyStore.ToolbarButtonNameKey, name, true);
            doc.SetField(KeyStore.LeftTappedOpsKey, new ListController<OperatorController> { op }, true);

            return doc;
        }
        private DocumentController             GetAppropriateSettingsDoc()
        {
            var settingsDoc = MainDocument.GetDataDocument().GetField<DocumentController>(KeyStore.SettingsDocKey);
            if (settingsDoc == null)
            {
                settingsDoc = GetDefaultSettingsDoc();
                MainDocument.GetDataDocument().SetField(KeyStore.SettingsDocKey, settingsDoc, true);
            }
            return settingsDoc;
        }
        private static DocumentController      GetDefaultSettingsDoc()
        {
            var settingsDoc = new DocumentController();
            settingsDoc.SetField<BoolController>  (KeyStore.SettingsNightModeKey, DashConstants.DefaultNightModeEngaged, true);
            settingsDoc.SetField<BoolController>  (KeyStore.SettingsUpwardPanningKey, DashConstants.DefaultInfiniteUpwardPanningStatus, true);
            settingsDoc.SetField<NumberController>(KeyStore.SettingsFontSizeKey, DashConstants.DefaultFontSize, true);
            settingsDoc.SetField<TextController>  (KeyStore.SettingsMouseFuncKey, SettingsView.MouseFuncMode.Zoom.ToString(), true);
            settingsDoc.SetField<TextController>  (KeyStore.SettingsWebpageLayoutKey, SettingsView.WebpageLayoutMode.Default.ToString(), true);
            settingsDoc.SetField<NumberController>(KeyStore.SettingsNumBackupsKey, DashConstants.DefaultNumBackups, true);
            settingsDoc.SetField<NumberController>(KeyStore.SettingsBackupIntervalKey, DashConstants.DefaultBackupInterval, true);
            settingsDoc.SetField<TextController>  (KeyStore.BackgroundImageStateKey, SettingsView.BackgroundImageState.Grid.ToString(), true);
            settingsDoc.SetField<NumberController>(KeyStore.BackgroundImageOpacityKey, 1.0, true);
            settingsDoc.SetField<BoolController>  (KeyStore.SettingsMarkdownModeKey, false, true);
            settingsDoc.SetField<TextController>  (KeyStore.AuthorKey, "New User", true);
            return settingsDoc;
        }
        #endregion

        private void CoreWindowOnKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (!xMainSearchBox.GetDescendants().Contains(FocusManager.GetFocusedElement()) &&
                !(FocusManager.GetFocusedElement() is RichEditBox || FocusManager.GetFocusedElement() is TextBox || FocusManager.GetFocusedElement() is MarkdownTextBlock ))
            {
                if (this.IsCtrlPressed())
                {
                    switch (e.VirtualKey)
                    {
                        case VirtualKey.Z: UndoManager.UndoOccured(); break;
                        case VirtualKey.Y: UndoManager.RedoOccured(); break;
                        case VirtualKey.A: SelectionManager.SelectDocuments(null, this.IsShiftPressed()); break;
                        case VirtualKey.F: OpenSearchBox(); break;
                    }
                }
                else if (e.VirtualKey == VirtualKey.Back || e.VirtualKey == VirtualKey.Delete)
                {
                    SelectionManager.DeleteSelected();
                } 
                else if (e.VirtualKey == VirtualKey.Escape)
                {
                    SinkFocus();
                }
            }
            //if (xTabCanvas.Children.Contains(TabMenu.Instance)) { TabMenu.Instance.HandleKeyDown(sender, e); }
        }
        
        private void HoverHighlightKey(object sender, KeyRoutedEventArgs e)       
        {
            var over = SelectionManager.SelectedDocViews.FirstOrDefault();
            if (over != null)
            {
                var scrPos = over.GetBoundingRect(xOuterGrid);
                XDocumentHover.Width = scrPos.Width;
                XDocumentHover.Height = scrPos.Height;
                XDocumentHover.RenderTransform = new TranslateTransform() { X = scrPos.X, Y = scrPos.Y };
                XHoverOutline.StrokeThickness = 1;
            }
            else
            {
                XHoverOutline.StrokeThickness = 0;
            }
        }
        private void HoverHighlight(object sender, PointerRoutedEventArgs e)       
        {
            PointerRoutedArgsHack = e;
            var over = VisualTreeHelper.FindElementsInHostCoordinates(e.GetCurrentPoint(null).Position, xOuterGrid).ToList().OfType<DocumentView>().FirstOrDefault();
            if (over != null)
            {
                var scrPos = over.GetBoundingRect(xOuterGrid);
                XDocumentHover.Width = scrPos.Width;
                XDocumentHover.Height = scrPos.Height;
                XDocumentHover.RenderTransform = new TranslateTransform() { X = scrPos.X, Y = scrPos.Y };
                XHoverOutline.StrokeThickness = 1;
            }
            else
            {
                XHoverOutline.StrokeThickness = 0;
            }
        }

        private void xSearchButton_Clicked(object sender, RoutedEventArgs tappedRoutedEventArgs)
        {
            if (xSearchBoxGrid.Visibility == Visibility.Visible)
            {
                xFadeAnimationOut.Begin();
                xSearchBoxGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                xSearchBoxGrid.Visibility = Visibility.Visible;
                xFadeAnimationIn.Begin();
                xMainSearchBox.Focus(FocusState.Programmatic);
            }
        }
        private void xSettingsButton_Clicked(object sender, RoutedEventArgs e)     
        {
            xToolbar.ChangeVisibility(xSettingsView.Visibility != Visibility.Collapsed);
            xSettingsView.Visibility = xToolbar.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }
        private void xPresentationButton_Clicked(object sender, RoutedEventArgs e) { UIFunctions.TogglePresentation(); }
        private void xBackButton_Clicked(object sender, RoutedEventArgs e)         { SplitFunctions.FrameHistoryBack(); }
        private void xForwardButton_Clicked(object sender, RoutedEventArgs e)      { SplitFunctions.FrameHistoryForward(); }
        private void xUndoButton_Clicked(object sender, RoutedEventArgs e)         { UndoManager.UndoOccured(); }
        private void xRedoButton_Clicked(object sender, RoutedEventArgs e)         { UndoManager.RedoOccured(); }
        private void xToolbarButton_Clicked(object sender, RoutedEventArgs e)      { xToolbar.Visibility = xToolbar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }

        private void Timeline_OnCompleted(object sender, object e)                 { xSnapshotOverlay.Visibility = Visibility.Collapsed; }

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

        public LinkHandledResult   HandleLink(DocumentController linkDoc, LinkDirection direction)
        {
            var region = linkDoc.GetDataDocument().GetLinkedDocument(direction);
            var target = region.GetRegionDefinition() ?? region;

            if (this.IsCtrlPressed() && !this.IsAltPressed())
            {
                NavigateToDocumentOrRegion(region, linkDoc);
                return LinkHandledResult.HandledClose;
            }
            var onScreenView = FindTargetDocumentView(target);

            if (target.GetLinkBehavior() == LinkBehavior.Overlay)
            {
                target.GotoRegion(region, linkDoc);
                onScreenView?.ViewModel.SetSearchHighlightState(true);
                return LinkHandledResult.HandledClose;
            }

            if (linkDoc.GetLinkBehavior() == LinkBehavior.ShowRegion)
            {
                AddFloatingDoc(linkDoc.GetDataDocument().GetLinkedDocument(LinkDirection.ToSource));
            }

            if (onScreenView != null) // we found the hyperlink target being displayed somewhere *onscreen*.  If it's hidden, show it.  If it's shown in the main workspace, hide it. If it's show in a docked pane, remove the docked pane.
            {
                ShowTarget(target, linkDoc, region);
            }
            else
            {
                ToggleFloatingDoc(target);
                target.GotoRegion(region, linkDoc);
            }

            return LinkHandledResult.HandledClose;
        }

        public static void ShowTarget(DocumentController target, DocumentController linkDoc, DocumentController region)
        {
            var onScreenView = MainPage.Instance.FindTargetDocumentView(target);
            var highlighted = onScreenView.ViewModel.SearchHighlightState != DocumentViewModel.UnHighlighted;
            if (highlighted && (target.Equals(region) || target.GetField<DocumentController>(KeyStore.GoToRegionKey)?.Equals(region) == true || region == null)) // if the target is a document or a visible region ...
            {
                onScreenView.ViewModel.LayoutDocument.ToggleHidden();
            }
            else // otherwise, it's a hidden region that we have to show
            {
                onScreenView.ViewModel.LayoutDocument.SetHidden(false);
            }
            if (onScreenView.Visibility == Visibility.Visible)
            {
                onScreenView.ViewModel.LayoutDocument.GotoRegion(region, linkDoc);
                onScreenView.ViewModel.SetSearchHighlightState(true);
            }
            else
            {
                onScreenView.ViewModel.SetSearchHighlightState(false);
                onScreenView.GetDescendantsOfType<AnnotationOverlay>().ToList().ForEach((ann) => ann.DeselectRegions());
            }
        }

        private void               NavigateToDocument(DocumentController doc)//More options
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
        private void               NavigateToDocumentOrRegion(DocumentController docOrRegion, DocumentController link = null)//More options
        {
            DocumentController parent = docOrRegion.GetRegionDefinition();
            (parent ?? docOrRegion).SetHidden(false);
            NavigateToDocument(parent ?? docOrRegion);
            if (parent != null)
            {
                parent.GotoRegion(docOrRegion, link);
            }
        }
        public void                ToggleFloatingDoc(DocumentController doc)
        {
            var onScreenView = FindTargetDocumentView(doc);

            if (onScreenView != null)
            {
                var highlighted = onScreenView.ViewModel.SearchHighlightState != DocumentViewModel.UnHighlighted;
                onScreenView.ViewModel.SetSearchHighlightState(true);
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
        private DocumentView       FindTargetDocumentView(DocumentController target)
        {
            //TODO Do this search the other way around, only checking documents in view instead of checking all documents and then seeing if it is in view
            var dataDoc  = target.GetDataDocument();
            var docViews = MainSplitter.GetDescendantsOfType<DocumentView>().Where(v => v.ViewModel != null && v.ViewModel.DataDocument.Equals(dataDoc)).ToList();
            
            foreach (var view in docViews)
            {
                var found = view;
                foreach (var parentView in view.GetAncestorsOfType<DocumentView>())
                {
                    var transformedBounds = view.TransformToVisual(parentView) .TransformBounds(new Rect(0, 0, view.ActualWidth, view.ActualHeight));
                    var parentBounds = new Rect(0, 0, parentView.ActualWidth, parentView.ActualHeight);
                    bool containsTL = parentBounds.Contains(new Point(transformedBounds.Left,  transformedBounds.Top));
                    bool containsBR = parentBounds.Contains(new Point(transformedBounds.Right, transformedBounds.Bottom));
                    bool containsTR = parentBounds.Contains(new Point(transformedBounds.Right, transformedBounds.Top));
                    bool containsBL = parentBounds.Contains(new Point(transformedBounds.Left,  transformedBounds.Bottom));
                    if (!(containsTL || containsBR || containsBL || containsTR))
                    {
                        found = null;
                    }
                }
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private async Task<string> processTemplate(IEnumerable<DocumentController> docs, TemplateType templateType)
        {
            var fields = docs.Select(d => d.GetDataDocument().EnumDisplayableFields().Select(f => f.Key.Name)).Aggregate((a, b) => a.Intersect(b)).ToList();
            fields.Insert(0, "");
            fields.Add("Caption"); // bcz: need default set of fields + ability to let user define a new default field

            ICustomTemplate templatePopup = null;
            switch (templateType)
            {
                case TemplateType.Citation:       templatePopup = new CitationPopup(fields); break;
                case TemplateType.CaptionedImage: templatePopup = new CaptionedImage(fields); break;
                case TemplateType.Note:           templatePopup = new NotePopup(fields); break;
                case TemplateType.Card:           templatePopup = new CardPopup(fields); break;
                case TemplateType.Title:          templatePopup = new TitlePopup(fields); break;
                case TemplateType.Profile:        templatePopup = new ProfilePopup(fields); break;
                case TemplateType.Article:        templatePopup = new ArticlePopup(fields); break;
                case TemplateType.Biography:      templatePopup = new BiographyPopup(fields); break;
                case TemplateType.Flashcard:      templatePopup = new FlashcardPopup(fields); break;
            }
            SetUpPopup(templatePopup);
            var customLayout = await templatePopup.GetLayout();
            UnsetPopup();

            if (customLayout != null)
            {
                var templateXaml = TemplateList.Templates[(int)templateType].GetXaml();
                var splitXaml    = templateXaml.Split(" ", StringSplitOptions.None);
                for (int i = 0; i < customLayout.Count; i++)
                {
                    for (int j = 0; j < splitXaml.Length; j++)
                    {
                        if (splitXaml[j].Contains("Field" + i))
                        {
                            splitXaml[j] = splitXaml[j].Replace(i + "", customLayout[i]);
                            break;
                        }
                        if (splitXaml[j].Contains("PlaceHolderText" + i))
                        {
                            splitXaml[j] = splitXaml[j].Replace("PlaceHolderText" + i, customLayout[i]);
                            break;
                        }
                    }
                }

                return string.Join(" ", splitXaml);
            }
            return null;
        }



        //private void CoreWindowOnKeyUp(CoreWindow sender, KeyEventArgs e)
        //{
        //if (e.VirtualKey == VirtualKey.Tab && !(FocusManager.GetFocusedElement() is RichEditBox) &&
        //    !(FocusManager.GetFocusedElement() is TextBox))
        //{
        //    var pos = this.RootPointerPos();
        //    var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(pos, this).OfType<CollectionView>().ToList();
        //    if (topCollection.FirstOrDefault()?.CurrentView is CollectionFreeformView freeformView)
        //    {
        //        TabMenu.ConfigureAndShow(freeformView, new Point(pos.X - xTreeMenuColumn.ActualWidth, pos.Y), xTabCanvas, true);
        //        TabMenu.Instance?.AddGoToTabItems();
        //    }
        //}

        //// TODO propagate the event to the tab menu
        //if (xTabCanvas.Children.Contains(TabMenu.Instance))
        //{
        //    TabMenu.Instance.HandleKeyUp(sender, e);
        //}

        //    e.Handled = true;
        //}
    }
}
