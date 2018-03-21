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
using Microsoft.Toolkit.Uwp.UI;
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

        public RadialMenuView       RadialMenu {get;set;}
        public BrowserView          WebContext => BrowserView.Current;
        public DocumentController   MainDocument { get; private set; }
        public DocumentView         MainDocView { get { return xMainDocView; } set { xMainDocView = value; } }
        public static InkController InkController { get; set; } = new InkController();

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

            RadialMenu = new RadialMenuView(xCanvas);
            RadialMenu.Loaded += (s,e) => RadialMenu.JumpToPosition(3 * ActualWidth / 4, 3 * ActualHeight / 4);

            Loaded += (s, e) =>
            {
                GlobalInkSettings.Hue = 200;
                GlobalInkSettings.Brightness = 30;
                GlobalInkSettings.Size = 4;
                GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Pen;
                GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pen;
                GlobalInkSettings.Opacity = 1;

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
            }

            await RESTClient.Instance.Fields.GetDocumentsByQuery<DocumentModel>(
                new DocumentTypeLinqQuery(DashConstants.TypeStore.MainDocumentType), Success, ex => throw ex);

            
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
            workspace = workspace.MakeDelegate();
            workspace.SetWidth(double.NaN);
            workspace.SetHeight(double.NaN);
            var documentViewModel = new DocumentViewModel(workspace);
            MainDocView.DataContext = documentViewModel;
            MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.WorkspaceHistoryKey).Add(currentWorkspace);
            MainDocument.SetField(KeyStore.LastWorkspaceKey, workspace, true);
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
                workspace = workspace.MakeDelegate();
                workspace.SetWidth(double.NaN);
                workspace.SetHeight(double.NaN);
                var documentViewModel = new DocumentViewModel(workspace);
                MainDocView.DataContext = documentViewModel;
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
                if (dm.DocumentController.GetDataDocument().Equals(document.GetDataDocument()))
                {
                    var containerViewModel = rootViewModel ?? dm;
                    var canvas = root.xItemsControl.ItemsPanelRoot as Canvas;
                    var center = new Point((MainDocView.ActualWidth - xMainTreeView.ActualWidth) / 2, MainDocView.ActualHeight / 2);
                    var shift = canvas.TransformToVisual(MainDocView).TransformPoint(
                        new Point(
                            containerViewModel.XPos + containerViewModel.ActualWidth / 2,
                            containerViewModel.YPos + containerViewModel.ActualHeight / 2));

                    

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
                            containerViewModel.XPos + containerViewModel.ActualWidth / 2,
                            containerViewModel.YPos + containerViewModel.ActualHeight / 2));
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
            if (topCollection.First().CurrentView is CollectionFreeformView)
            {
                TabMenu.ConfigureAndShow(topCollection.First().CurrentView as CollectionFreeformView, pos, xCanvas, true);
                TabMenu.Instance?.AddGoToTabItems();
                if (e != null)
                    e.Handled = true;
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
        private void TextBlock_GettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            args.Cancel = true;
        }
    }
}