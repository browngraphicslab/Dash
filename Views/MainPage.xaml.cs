using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Dash.Controllers;
using DashShared;
using DashShared.Models;
using Flurl;
using Flurl.Http;
using Flurl.Http.Content;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Visibility = Windows.UI.Xaml.Visibility;
using static Dash.NoteDocuments;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.UI;
using Dash.Views.Collection;
using Dash.Views.Document_Menu;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;


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
                    var documentController = new CollectionNote(new Point(0, 0),
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

        public void SetCurrentWorkspace(DocumentController workspace)
        {
            workspace.SetWidth(double.NaN);
            workspace.SetHeight(double.NaN);
            var documentViewModel = new DocumentViewModel(workspace);
            xMainDocView.DataContext = documentViewModel;
            documentViewModel.SetSelected(null, true);
            MainDocument.SetField(KeyStore.LastWorkspaceKey, workspace, true);
        }

        public bool NavigateToDocumentInWorkspace(DocumentController document)
        {
            var dvm = xMainDocView.DataContext as DocumentViewModel;
            var coll = (dvm.Content as CollectionView)?.CurrentView as CollectionFreeformView;
            if (coll != null)
                return NavigateToDocument(coll, null, coll, document);
            return false;
        }

        public bool NavigateToDocument(CollectionFreeformView root, DocumentViewModel rootViewModel, CollectionFreeformView collection, DocumentController document)
        {
            foreach (var dm in collection.ViewModel.DocumentViewModels)
                if (dm.DocumentController.Equals(document))
                {
                    var containerViewModel = rootViewModel ?? dm;
                    var canvas = root.xItemsControl.ItemsPanelRoot as Canvas;
                    var center = new Point((xMainDocView.ActualWidth - xMainTreeView.ActualWidth) / 2, xMainDocView.ActualHeight / 2);
                    var shift = canvas.TransformToVisual(xMainDocView).TransformPoint(
                        new Point(
                            containerViewModel.GroupTransform.Translate.X + containerViewModel.Width / 2,
                            containerViewModel.GroupTransform.Translate.Y + containerViewModel.Height / 2));
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

        private void MyGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            xMainDocView.Width = e.NewSize.Width;
            xMainDocView.Height = e.NewSize.Height;
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

        #region Requests

        private enum HTTPRequestMethod
        {
            Get,
            Post
        }

        private async void TwitterTestButtonOnTapped(object sender, TappedRoutedEventArgs e)
        {
            // authorization on the tiwtter api
            var twitterBase = "https://api.twitter.com";
            var twitterAuthEndpoint = twitterBase.AppendPathSegments("oauth2", "token");
            var twitterConsumerKey = "GSrTmog2xY7PWzxSjfGQDuKAH";
            var twitterConsumerSecret = "6QOcnCElbr4u80tiWspoGQTYryFyyRoXxMgiSZv4fq0Fox3dhV";
            var token = await OAuth2Authentication(twitterAuthEndpoint, twitterConsumerKey, twitterConsumerSecret);

            var userName = "alanalevinson";
            var tweetsByUserURL = twitterBase.AppendPathSegments("1.1", "statuses", "user_timeline.json").SetQueryParams(new { screen_name = userName, count = 25, trim_user = "true" });
            var tweetsByUser = await MakeRequest(tweetsByUserURL, HTTPRequestMethod.Get, token);

            var responseAsDocument = new JsonToDashUtil().ParseJsonString(tweetsByUser, tweetsByUserURL.ToString(true));
            DisplayDocument(responseAsDocument);

        }

        private async Task<string> MakeRequest(string baseUrl, HTTPRequestMethod method, string token = null)
        {
            return await MakeRequest(new Url(baseUrl), method, token);
        }

        private async Task<string> MakeRequest(Url baseUrl, HTTPRequestMethod method, string token = null)
        {
            IFlurlClient client = new FlurlClient(baseUrl);
            //Authorization header with the value of Bearer <base64 bearer token value from step 2>
            if (token != null)
            {
                var encodedToken = Base64Encode(token);
                client = baseUrl.WithHeader("Authorization", $"Bearer {token}");
            }

            HttpResponseMessage response = null;
            if (method == HTTPRequestMethod.Get)
            {
                response = await client.GetAsync();
            }
            else if (method == HTTPRequestMethod.Post)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException();
            }

            return await GetStringFromResponseAsync(response);

        }

        private async Task<string> OAuth2Authentication(string authBaseUrl, string consumerKey, string consumerSecret)
        {
            return await OAuth2Authentication(new Url(authBaseUrl), consumerKey, consumerSecret);
        }


        private async Task<string> OAuth2Authentication(Url authBaseUrl, string consumerKey, string consumerSecret)
        {
            var concatKeySecret = $"{consumerKey}:{consumerSecret}";
            var encodedKeySecret = Base64Encode(concatKeySecret);

            var authResponse = await authBaseUrl.WithHeader("Authorization", $"Basic {encodedKeySecret}")
                .PostUrlEncodedAsync(new
                {
                    grant_type = "client_credentials"
                });

            // get the string from the response, including decompression and checking for success
            var responseString = await GetStringFromResponseAsync(authResponse);

            if (responseString != null)
            {
                return JObject.Parse(responseString).GetValue("access_token").Value<string>();
            }
            return null;
        }

        private async Task<string> GetStringFromResponseAsync(HttpResponseMessage authResponse)
        {
            if (authResponse != null && authResponse.IsSuccessStatusCode)
            {
                if (authResponse.Content.Headers.ContentEncoding.Contains("gzip"))
                {
                    var bytes = await authResponse.Content.ReadAsByteArrayAsync();
                    return Unzip(bytes);
                }
                return await authResponse.Content.ReadAsStringAsync();
            }
            return null;
        }

        public string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }


        #endregion

        private void Border_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }
        
        public void ThemeChange()
        {
            this.RequestedTheme = this.RequestedTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        }


        private void CollapseButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            TreeMenuColumn.Width = new GridLength(300 - TreeMenuColumn.Width.Value);
            if (Math.Abs(TreeMenuColumn.Width.Value) < 0.0001)
            {
                CollapseButton.Text = ">";
            }
            else
            {
                CollapseButton.Text = "<";
            }
        }
    }
}