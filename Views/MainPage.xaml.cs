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
using Windows.Storage;
using Dash.Popups;
using Color = Windows.UI.Color;
using Point = Windows.Foundation.Point;
using System.Web;
using MyToolkit.Multimedia;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    ///     Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom.
    /// </summary>
    public sealed partial class MainPage : Page, ILinkHandler
    {
        static public Windows.UI.Input.PointerPoint PointerCaptureHack;  // saves a PointerPoint to be used for switching from a UWP manipulation to a Windows Drag Drop

        public enum PresentationViewState
        {
            Expanded,
            Collapsed
        }


        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;
        public static MainPage Instance { get; private set; }

        public BrowserView WebContext => BrowserView.Current;
        public DocumentController MainDocument { get; private set; }
        public DocumentView MainDocView { get => xMainDocView; set => xMainDocView = value; }
        public DockingFrame DockManager => xDockFrame;
	    public LinkActivationManager ActivationManager = new LinkActivationManager();

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

        public DashPopup ActivePopup;
        public Grid SnapshotOverlay => xSnapshotOverlay;
        public Storyboard FadeIn => xFadeIn;
        public Storyboard FadeOut => xFadeOut;

        public static PointerRoutedEventArgs PointerRoutedArgsHack = null;
        public MainPage()
        {
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            //formattableTitleBar.ButtonBackgroundColor = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]).Color;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = false;
            AddHandler(PointerMovedEvent, new PointerEventHandler((s,e) => PointerRoutedArgsHack = e), true); 
            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;

            InitializeComponent();
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

            xDockFrame.Loaded += (s, e) => xDockFrame.DocController = MainDocument;

            xSplitter.Tapped += (s, e) => xTreeMenuColumn.Width = Math.Abs(xTreeMenuColumn.Width.Value) < .0001 ? new GridLength(300) : new GridLength(0);
            xBackButton.Tapped += (s, e) => GoBack();
            xForwardButton.Tapped += (s, e) => GoForward();
            Window.Current.CoreWindow.KeyUp += CoreWindowOnKeyUp;
            Window.Current.CoreWindow.KeyDown += CoreWindowOnKeyDown;

            Window.Current.CoreWindow.SizeChanged += (s, e) =>
            {
                double newHeight = e.Size.Height;
                double newWidth = e.Size.Width;
	            if (ActivePopup != null)
	            {
		            ActivePopup.SetHorizontalOffset((newWidth / 2) - 200 - (xLeftGrid.ActualWidth / 2));
		            ActivePopup.SetVerticalOffset((newHeight / 2) - 150);
				}
            };

            xToolbar.SetValue(Canvas.ZIndexProperty, 20);

            xLinkInputBox.AddKeyHandler(VirtualKey.Escape, args => { HideLinkInputBox(); });
            xLinkInputBox.LostFocus += (sender, args) => { HideLinkInputBox(); };
        }

        private void HideLinkInputBox()
        {
            xLinkInputBox.ClearHandlers(new[] { VirtualKey.Enter });
            xLinkInputOut.Begin();
            xLinkInputOut.Completed += (o, o1) =>
            {
                xLinkInputBox.Text = "";
                xLinkInputBox.Visibility = Visibility.Collapsed;
            };
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
                        var layout = new CollectionBox(new DocumentReferenceController(MainDocument, KeyStore.DataKey)).Document;
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
                    var layout = new CollectionBox(new DocumentReferenceController(MainDocument, KeyStore.DataKey)).Document;
                    MainDocument.SetActiveLayout(layout, true, true);
                }
                LoadSettings();

                var presentationItems = MainDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.PresentationItemsKey, null);
                xPresentationView.DataContext = presentationItems != null ? new PresentationViewModel(presentationItems) : new PresentationViewModel();

                var col = MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.DataKey);
                var history =
                    MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceHistoryKey);
                xDockFrame.DocController = MainDocument;
                xDockFrame.LoadDockedItems();
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

                MainDocView.ViewModel = new DocumentViewModel(lastWorkspace) { DecorationState = false };

                var treeContext = new CollectionViewModel(MainDocument, KeyStore.DataKey);
                xMainTreeView.DataContext = treeContext;
                xMainTreeView.ChangeTreeViewTitle("Workspaces");
                //xMainTreeView.ToggleDarkMode(true);

                setupMapView(lastWorkspace);

                if (CurrPresViewState == PresentationViewState.Expanded) SetPresentationState(true);
            }

            await DotNetRPC.Init();

            await RESTClient.Instance.Fields.GetDocumentsByQuery<DocumentModel>(
                new DocumentTypeLinqQuery(DashConstants.TypeStore.MainDocumentType), Success, ex => throw ex);

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
            var settingsDoc = MainDocument.GetField<DocumentController>(KeyStore.SettingsDocKey);
            if (settingsDoc != null) return settingsDoc;
            Debug.WriteLine("GETTING DEFAULT");
            settingsDoc = GetDefaultSettingsDoc();
            MainDocument.SetField(KeyStore.SettingsDocKey, settingsDoc, true);
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

            return settingsDoc;
        }

        #endregion

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

            if (currentWorkspace.GetDataDocument().Equals(workspace.GetDataDocument()))
            {
                return true;
            }
            var workspaceView = double.IsNaN(workspace.GetWidthField()?.Data ?? 0) ?  workspace.GetActiveLayout() ?? workspace : workspace.GetViewCopy();
            workspaceView.SetWidth(double.NaN);
            workspaceView.SetHeight(double.NaN);
            MainDocView.DataContext = new DocumentViewModel(workspaceView);
            if (workspaceView.DocumentType.Equals(CollectionBox.DocumentType))
            {
                workspaceView.SetFitToParent(false);
                setupMapView(workspaceView);
            }

            MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceHistoryKey).Add(currentWorkspace);
            MainDocument.SetField(KeyStore.LastWorkspaceKey, workspaceView, true);
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
                MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceFutureKey)
                    .Add(MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey));
                MainDocView.DataContext = new DocumentViewModel(workspace);
                setupMapView(workspace);
                MainDocument.SetField(KeyStore.LastWorkspaceKey, workspace, true);
            }
        }

        public void GoForward()
        {
            var future =
                MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceFutureKey);
            if (future.Count > 0)
            {
                var workspace = future.TypedData.Last();
                future.Remove(workspace);
                MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceHistoryKey)
                    .Add(MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey));
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
                    var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformBase;
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
                                    NavigateToDocumentInWorkspace(document, false, false);
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
                                    if (!NavigateToDocumentInWorkspace(document, false, false))
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
                                    if (!NavigateToDocumentInWorkspace(document, false, false))
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
        public bool NavigateToDocumentInWorkspace(DocumentController document, bool animated, bool zoom, bool compareDataDocuments = false)
        {
            var dvm = MainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformBase;
            if (coll != null)
            {
                return NavigateToDocument(coll, null, coll, document, animated, zoom, compareDataDocuments);
            }
            return false;
        }

        public void HighlightTreeView(DocumentController document, bool? flag)
        {
            xMainTreeView.Highlight(document, flag);
        }

        public void HighlightDoc(DocumentController document, bool? flag, int search = 0, bool animate = false)
        {
            var dvm = MainDocView.ViewModel;
            highlightDoc(dvm, document, flag, search, animate);

            //foreach (DockedView dockedView in this.GetDescendantsOfType<DockedView>())
            //{
            //    highlightDoc(dockedView.ContainedDocumentView.ViewModel, document, flag, search, animate);
            //}
        }

        private void highlightDoc(DocumentViewModel dm, DocumentController document, bool? flag, int search, bool animate = false)
        {
            if (xMainTreeView.ViewModel.ViewLevel.Equals(CollectionViewModel.StandardViewLevel.Overview) || xMainTreeView.ViewModel.ViewLevel.Equals(CollectionViewModel.StandardViewLevel.Region)) return;
            if (dm.DocumentController.Equals(document))
            {
                //for search - 0 means no change, 1 means turn highlight on, 2 means turn highlight off
                if (search == 0)
                {
                    if (flag == null)
                    {
                        dm.DecorationState = (dm.Undecorated == false) && !dm.DecorationState;
                    }
                    else if (flag == true)
                    {
                        dm.DecorationState = (dm.Undecorated == false);
                        dm.SearchHighlightBrush = ColorConverter.HexToBrush("#e50000");
                    }
                    else if (flag == false)
                    {
                        dm.DecorationState = false;
                        dm.SearchHighlightBrush = ColorConverter.HexToBrush("#fffc84");
                    }
                }
                else if (search == 1)
                {
                    //highlight doc
                    if (animate)
                    {
                        dm.ExpandBorder();
                    }
                    else
                    {
                        dm.SearchHighlightState = new Thickness(8);
                    }
                }
                else
                {
                    //unhighlight doc
                    if (animate)
                    {
                        dm.RetractBorder();
                    }
                    else
                    {
                        dm.SearchHighlightState = new Thickness(0);
                    }
                }
            }
            else if (dm.Content is CollectionView && (dm.Content as CollectionView)?.CurrentView is CollectionFreeformBase freeformView)
            {
                foreach (var vm in freeformView.ViewModel.DocumentViewModels)
                {
                    highlightDoc(vm, document, flag, search, animate);
                }
            }
        }

        public bool NavigateToDocumentInWorkspaceAnimated(DocumentController document, bool zoom)
        {
            var dvm = MainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformBase;
            if (coll != null && document != null)
            {
                return NavigateToDocument(coll, null, coll, document, true, zoom, true);
            }
            return false;
        }

        public bool NavigateToDocument(CollectionFreeformBase root, DocumentViewModel rootViewModel, CollectionFreeformBase collection, 
            DocumentController document, bool animated, bool zoom, bool compareDataDocuments = false)
        {
            if (collection?.ViewModel?.DocumentViewModels == null || !root.IsInVisualTree())
            {
                return false;
            }

            var workspace = (MainDocView.DataContext as DocumentViewModel).DocumentController;
            var currentWorkspace = MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
            var workspaceView = workspace.GetViewCopy();
            workspaceView.SetWidth(double.NaN);
            workspaceView.SetHeight(double.NaN);

            MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceHistoryKey).Add(workspaceView);
            MainDocument.SetField(KeyStore.LastWorkspaceKey, currentWorkspace, true);

            //loop through each doc in collection
            foreach (var dm in collection.ViewModel.DocumentViewModels)
            {
                var dmd = dm.DocumentController.GetDataDocument();
                //if this doc is given document
                if (dm.DocumentController.Equals(document) || (compareDataDocuments && dm.DocumentController.GetDataDocument().Equals(document.GetDataDocument())))
                {
                    var containerViewModel = rootViewModel ?? dm;
                    var center = new Point(MainDocView.ActualWidth / 2, MainDocView.ActualHeight / 2);
                    //get center point of doc where you want to go
                    var shift = new Point(
                            containerViewModel.XPos + containerViewModel.ActualSize.X / 2,
                            containerViewModel.YPos + containerViewModel.ActualSize.Y / 2);

                    //get zoom changes
                    var shiftZ =new Point(containerViewModel.ActualSize.X / 2, containerViewModel.ActualSize.Y / 2);
                    
                   //get less zoom, so x and y are zoomed by same amt
                    var minZoom = Math.Min(center.X / shiftZ.X, center.Y / shiftZ.Y) * 0.9;
                    if (!zoom)
                    {
                        minZoom = root.ViewModel.TransformGroup.ScaleAmount.X;
                    }

                    if (animated)
                    {
                        //TranslateTransform moves object by x and y - find diff bt where you are (center) and where you want to go (shift)
                        root.SetTransformAnimated(
                            new TranslateTransform() { X = center.X - shift.X, Y = center.Y - shift.Y },
                            new ScaleTransform { CenterX = shift.X, CenterY = shift.Y, ScaleX = minZoom, ScaleY = minZoom } 
                        );
                    }
                    else root.SetTransform(new TranslateTransform() { X = center.X - shift.X, Y = center.Y - shift.Y }, null);
                    return true;
                }
                else if ((dm.Content as CollectionView)?.CurrentView is CollectionFreeformBase)
                {
                    if (NavigateToDocument(root, rootViewModel ?? dm, (dm.Content as CollectionView)?.CurrentView as CollectionFreeformBase, document, animated, compareDataDocuments))
                        return true;
                }
            }
            return false;
        }

        public Point GetDistanceFromMainDocCenter(DocumentController dc)
        {
            var dvm = MainDocView.DataContext as DocumentViewModel;
            var root = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformBase;

            var canvas = root.GetItemsControl().ItemsPanelRoot as Canvas;
            var center = new Point((MainDocView.ActualWidth - xMainTreeView.ActualWidth) / 2, MainDocView.ActualHeight / 2);
            var dcPoint = dc.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null).Data;
            var dcSize = dc.GetDereferencedField<PointController>(KeyStore.ActualSizeKey, null).Data;
            var shift = canvas.TransformToVisual(MainDocView).TransformPoint(new Point(
                dcPoint.X + dcSize.X / 2,
                dcPoint.Y + dcSize.Y / 2
            ));

            Debug.WriteLine(new Point(center.X - shift.X, center.Y - shift.Y));
            return new Point(center.X - shift.X, center.Y - shift.Y);
        }

        private void CoreWindowOnKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (e.Handled || xMainSearchBox.GetDescendants().Contains(FocusManager.GetFocusedElement()))
                return;

            if (!(FocusManager.GetFocusedElement() is RichEditBox || FocusManager.GetFocusedElement() is TextBox))
            {
                var ctrlDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                if (ctrlDown)
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
	        if (e.VirtualKey == VirtualKey.Escape )
	        {
		        using (UndoManager.GetBatchHandle())
		        {
			        ActivationManager.DeactivateAll();
				}
				
	        }

	        //activateall selected docs
	        if (e.VirtualKey == VirtualKey.A && this.IsShiftPressed())
	        {
		        var selected = SelectionManager.GetSelectedDocs();
		        if (selected.Count > 0)
		        {
			        using (UndoManager.GetBatchHandle())
			        {
						foreach (var doc in SelectionManager.GetSelectedDocs())
					        {
						        ActivationManager.ActivateDoc(doc);
					        }
						}
				        
			        }
			}

			var dvm = MainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformBase;

            // TODO: this should really only trigger when the marquee is inactive -- currently it doesn't happen fast enough to register as inactive, and this method fires
            // bcz: needs to be in keyUp because when typing in a new textBox inside a nested collection, no one catches the KeyDown event and putting this in KeyDown
            //       would cause a collection to be created when typing a 'c'
            // bcz: needs to be in keyDown because of potential conflicts when releasing the ctrl key before the 'c' key which causes this to 
            //       create a collection around a PDF when you're just copying text
            if (!(FocusManager.GetFocusedElement() is RichEditBox) && coll != null && !coll.IsMarqueeActive && !(FocusManager.GetFocusedElement() is TextBox))
            {
                coll.TriggerActionFromSelection(e.VirtualKey, false);
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
            if (e.VirtualKey == VirtualKey.Tab && !(FocusManager.GetFocusedElement() is RichEditBox))
            {
                MainDocView_OnDoubleTapped(null, null);
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

        private void MainDocView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var pos = this.RootPointerPos();
            var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(pos, this).OfType<CollectionView>().ToList();
            if (topCollection.FirstOrDefault()?.CurrentView is CollectionFreeformBase freeformView)
            {
                if (e != null)
                {
                    foreach (var d in freeformView?.GetItemsControl().ItemsPanelRoot.Children)
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

                if (e == null || !e.Handled && this.IsCtrlPressed())
                {
                    TabMenu.ConfigureAndShow(freeformView, new Point(pos.X - xTreeMenuColumn.ActualWidth, pos.Y), xTabCanvas, true);
                    TabMenu.Instance?.AddGoToTabItems();
                    if (e != null)
                        e.Handled = true;
                }
            }
        }

        public void AddOperatorsFilter(ICollectionView collection, DragEventArgs e)
        {
            TabMenu.ConfigureAndShow(collection as CollectionFreeformBase, e.GetPosition(Instance), xTabCanvas);
        }

        public void AddGenericFilter(object o, DragEventArgs e)
        {
            if (!xTabCanvas.Children.Contains(GenericSearchView.Instance))
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
        void setupMapView(DocumentController mainDocumentCollection)
        {
            if (xMapDocumentView == null)
            {
                var xMap = ContentController<FieldModel>.GetController<DocumentController>("3D6910FE-54B0-496A-87E5-BE33FF5BB59C") ?? new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform).Document;
                xMap.SetFitToParent(true);
                xMap.SetWidth(double.NaN);
                xMap.SetHeight(double.NaN);
                xMapDocumentView = new DocumentView() { DataContext = new DocumentViewModel(xMap) {Undecorated = true}, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
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
                mapTimer.Tick += (ss, ee) =>
                {
                    var cview = xMapDocumentView.GetFirstDescendantOfType<CollectionView>();
                    cview?.ViewModel?.FitContents(cview);
                };
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

		        var mainFreeform = this.xMainDocView.GetFirstDescendantOfType<CollectionFreeformView>();
		        var mainFreeFormCanvas = mainFreeform?.GetItemsControl().GetFirstDescendantOfType<Canvas>();
		        var mainFreeformXf = ((mainFreeFormCanvas?.RenderTransform ?? new MatrixTransform()) as MatrixTransform)?.Matrix ?? new Matrix();
		        var mainDocCenter = new Point(MainDocView.ActualWidth / 2 / mainFreeformXf.M11, MainDocView.ActualHeight / 2 / mainFreeformXf.M22);
		        var mainScale = new Point(mainFreeformXf.M11, mainFreeformXf.M22);
		        mainFreeform?.SetTransformAnimated(
			        new TranslateTransform() { X = -mapPt.X + xMainDocView.ActualWidth / 2, Y = -mapPt.Y + xMainDocView.ActualHeight / 2 },
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
                        if (isChecked != null && (bool) isChecked) xPresentationView.ShowLines();
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
                SetCurrentWorkspace(doc);
                return;
            }

            var workspace = MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
            if (workspace.GetDataDocument().Equals(node.Parent.DataDocument))
            {
                NavigateToDocumentInWorkspace(doc, true, false);
            }
            else
            {
                SetCurrentWorkspaceAndNavigateToDocument(node.Parent.ViewDocument, doc);
            }
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

        public void AddFloatingDoc(DocumentController doc, Point? size = null, Point? position = null)
        {
            var onScreenView = xDockFrame.GetDescendantsOfType<DocumentView>().Where(v => v.ViewModel != null &&
                v.ViewModel.DataDocument.Equals(doc.GetDataDocument())).FirstOrDefault();
            if (onScreenView != null)
                return;

            //make doc view out of doc controller
            var docCopy = doc.GetViewCopy();
            docCopy.SetWidth(size?.X ?? 150);
            docCopy.SetHeight(size?.Y ?? 100);
            //put popup slightly left of center, so its not covered centered doc
            var defaultPt = new Point(xCanvas.RenderSize.Width / 2 - 250, xCanvas.RenderSize.Height / 2 - 50);
            docCopy.SetPosition(position ?? defaultPt);
            
            var docView = new DocumentView
            {
                DataContext = new DocumentViewModel(docCopy),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                BindRenderTransform = true
            };

            docView.DocumentDeselected += DocView_DocumentDeselected;
            xCanvas.Children.Add(docView);

            SelectionManager.Select(docView, false);
        }

        private void DocView_DocumentDeselected(DocumentView sender)
        {
            sender.DocumentDeselected -= DocView_DocumentDeselected;
            xCanvas.Children.Remove(sender);
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
            var onScreenView = GetTargetDocumentView(xDockFrame, target);

            if (target.GetLinkBehavior() == LinkBehavior.Overlay)
            {
                target.GotoRegion(region, linkDoc);
                SelectionManager.SelectionChanged -= SelectionManagerSelectionChanged;
                SelectionManager.SelectionChanged += SelectionManagerSelectionChanged;
                if (onScreenView != null) onScreenView.ViewModel.SearchHighlightState = new Thickness(8);
                return LinkHandledResult.HandledRemainOpen;
            }

            if (onScreenView != null) // we found the hyperlink target being displayed somewhere *onscreen*.  If it's hidden, show it.  If it's shown in the main workspace, hide it. If it's show in a docked pane, remove the docked pane.
            {
                SelectionManager.SelectionChanged -= SelectionManagerSelectionChanged;
                SelectionManager.SelectionChanged += SelectionManagerSelectionChanged;

                var highlighted = onScreenView.ViewModel.SearchHighlightState != new Thickness(0);
                onScreenView.ViewModel.SearchHighlightState = new Thickness(8);
                if (highlighted && (target.Equals(region) || target.GetField<DocumentController>(KeyStore.GoToRegionKey)?.Equals(region) == true)) // if the target is a document or a visible region ...
                {
                    if (onScreenView.GetFirstAncestorOfType<DockedView>() == xMainDocView.GetFirstDescendantOfType<DockedView>()) // if the document was on the main screen (either visible or hidden), we toggle it's visibility
                        target.ToggleHidden();
                    else DockManager.Undock(onScreenView.GetFirstAncestorOfType<DockedView>()); // otherwise, it was in a docked pane -- instead of toggling the target's visibility, we just removed the docked pane.
                  
                }
                else // otherwise, it's a hidden region that we have to show
                {
                    target.SetHidden(false);
                }
            }
            else
            {
                Dock_Link(linkDoc, direction);
            }

            target.GotoRegion(region, linkDoc);

            void SelectionManagerSelectionChanged(DocumentSelectionChangedEventArgs args)
            {
                onScreenView?.ViewModel?.RetractBorder();
                SelectionManager.SelectionChanged -= SelectionManagerSelectionChanged;
            }

            return LinkHandledResult.HandledRemainOpen;
        }

        public void Dock_Link(DocumentController linkDoc, LinkDirection direction, bool inContext = true)
        {
            var region = linkDoc.GetDataDocument().GetLinkedDocument(direction);
            var target = region.GetRegionDefinition() ?? region;
            var onScreenView = GetTargetDocumentView(xDockFrame, target);

            DockedView docked = DockManager.GetDockedView(target); // if a document view matches this document's data document, then undock the view.
            if (docked != null)
            {
                DockManager.Undock(docked);
            }
            else  // otherwise, we have to show the document in a docked view
            {
                var tree = DocumentTree.MainPageTree;
                var node = tree.Where(n => n.ViewDocument.Equals(target)).FirstOrDefault();
                var collection = node?.Parent.ViewDocument;

                if (collection == null || !inContext)       // if the document doesn't exist in any collection, then just dock it by itself
                {
                    var docview = DockManager.Dock(target, DockDirection.Right);
                }
                else             // otherwise, find the collection that the document's in, and dock it.  It's possible the document was somewhere on the main view but not visible in which case this amounts to creating a split screen of the main view.
                {

                    DockedView dockedCollection = DockManager.GetDockedView(collection);
                    if (dockedCollection != null)
                    {
                        onScreenView = dockedCollection.GetDescendantsOfType<DocumentView>()
                            .Where((dv) => dv.ViewModel.LayoutDocument.Equals(target)).FirstOrDefault();
                        if (onScreenView != null && onScreenView.ViewModel.SearchHighlightState != new Thickness(8))
                            onScreenView.ViewModel.SearchHighlightState = new Thickness(8);
                        else DockManager.Undock(dockedCollection);
                    }
                    else
                    {

                        target.SetHidden(false);
                        var docView = DockManager.Dock(collection, DockDirection.Right);
                        var cview = docView.ViewModel.Content;
                        cview.Tag = target;
                        cview.Loaded += Docview_Loaded;
                        var col = docView.ViewModel.DocumentController;

                        var pos = node.ViewDocument.GetPosition() ?? new Point();
                        double xZoom = 500 / (node.ViewDocument.GetActualSize()?.X ?? 500);
                        double YZoom = MainDocView.ActualHeight /
                                       (node.ViewDocument.GetActualSize()?.Y ?? MainDocView.ActualHeight);
                        var zoom = Math.Min(xZoom, YZoom) * 0.7;
                        //col.SetField<PointController>(KeyStore.PanPositionKey,
                        //    new Point((250 - pos.X - (node.ViewDocument.GetActualSize()?.X ?? 0) / 4) * zoom, (MainDocView.ActualHeight / 2 - (pos.Y - node.ViewDocument.GetActualSize()?.Y ?? 0) / 2) * zoom), true);
                        double xOff = 500 - (node.ViewDocument.GetActualSize()?.X ?? 0) * zoom;
                        double yOff = MainDocView.ActualHeight - (node.ViewDocument.GetActualSize()?.Y ?? 0) * zoom;
                        double xrat = 500 / (double)(node.ViewDocument.GetActualSize()?.X);
                        col.SetField<PointController>(KeyStore.PanPositionKey,
                            new Point(-pos.X * zoom + 0.3 * xrat * xOff, -pos.Y * zoom + 0.4 * yOff), true);

                        col.SetField<PointController>(KeyStore.PanZoomKey,
                            new Point(zoom, zoom), true);
                    }
                }
            }
        }

        private void Docview_Loaded(object sender, RoutedEventArgs e)
        {
            var cview = (sender as CollectionView);
            foreach (var doc in cview.ViewModel.DocumentViewModels)
                if (doc.DocumentController.Equals(cview.Tag as DocumentController))
                {
                    SelectionManager.SelectionChanged -= SelectionManagerSelectionChanged;
                    SelectionManager.SelectionChanged += SelectionManagerSelectionChanged;
                    doc.SearchHighlightState = new Thickness(8);
                    void SelectionManagerSelectionChanged(DocumentSelectionChangedEventArgs args)
                    {
                        doc.SearchHighlightState = new Thickness(0);
                        SelectionManager.SelectionChanged -= SelectionManagerSelectionChanged;
                    }
                }


            cview.Loaded -= Docview_Loaded;
        }

        public DocumentView GetTargetDocumentView(DockingFrame frame, DocumentController target)
        {
            //TODO Do this search the other way around, only checking documents in view instead of checking all documents and then seeing if it is in view
            var docViews = frame.GetDescendantsOfType<DocumentView>().Where(v => v.ViewModel != null && v.ViewModel.LayoutDocument.Equals(target)).ToList();
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
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
        }

        private void XOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
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

        public void TogglePopup()
        {
            //xLoadingPopup.HorizontalOffset = ((Frame)Window.Current.Content).ActualWidth / 2 - 200 - (xLeftGrid.ActualWidth / 2);
            //xLoadingPopup.VerticalOffset = ((Frame)Window.Current.Content).ActualHeight / 2 - 150;
            //xLoadingPopup.IsOpen = true;
            //Load.Begin();
        }

        public void ClosePopup()
        {
            //Load.Stop();
            //xLoadingPopup.HorizontalOffset = 0;
            //xLoadingPopup.VerticalOffset = 0;
            //xLoadingPopup.IsOpen = false;

        }

        private ToolTip _search;
        private ToolTip _back;
        private ToolTip _forward;
        private ToolTip _presentation;
        private ToolTip _export;

        private void SetUpToolTips()
        {
            const PlacementMode placementMode = PlacementMode.Bottom;
            const int offset = 5;

            _search = new ToolTip()
            {
                Content = "Search workspace",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xSearchButton, _search);

            _back = new ToolTip()
            {
                Content = "Go back",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xBackButton, _back);

            _forward = new ToolTip()
            {
                Content = "Go forward",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xForwardButton, _forward);
        }

        private async void MakePdf_OnTapped(object sender, TappedRoutedEventArgs e)
        {
           xMainTreeView.MakePdf_OnTapped(sender, e);
        }

        private void TogglePresentationMode(object sender, TappedRoutedEventArgs e)
        {
           xMainTreeView.TogglePresentationMode(sender, e);
        }

        private void Snapshot_OnTapped(object sender, TappedRoutedEventArgs e)
        {
           xMainTreeView.Snapshot_OnTapped(sender, e);
        }



    }
}
