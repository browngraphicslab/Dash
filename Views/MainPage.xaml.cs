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
using Dash.Views.Document_Menu;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    ///     Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Instance { get; private set; }

        public static InkRecognitionSubHelper InkRecognizer { get; private set; }

        private RadialMenuView _radialMenu;
        private static CollectionView _mainCollectionView;
        private Flyout OperatorMenuFlyout;
        public DocumentView MainDocView { get { return xMainDocView; } set { xMainDocView = value; } }
        public RadialMenuView RadialMenu => _radialMenu;
        public DocumentController MainDocument { get; private set; }
        public static InkController InkController = new InkController();
        public AddMenu AddMenu { get { return xAddMenu; } set { xAddMenu = value; } }
        public MainPage()
        {
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]).Color;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = false;
            InitializeComponent();

            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;

            InkRecognizer = new InkRecognitionSubHelper();

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
                        [KeyStore.CollectionKey] = new ListController<DocumentController>()
                    };
                    MainDocument = new DocumentController(fields, DashConstants.TypeStore.MainDocumentType);

                    var layout = new CollectionBox(new DocumentReferenceController(MainDocument.GetId(), KeyStore.CollectionKey)).Document;
                    MainDocument.SetActiveLayout(layout, true, true);
                }
                xMainDocView.DataContext = new DocumentViewModel(MainDocument);
            }

            await RESTClient.Instance.Fields.GetDocumentsByQuery<DocumentModel>(
                new DocumentTypeLinqQuery(DashConstants.TypeStore.MainDocumentType), Success, ex => throw ex);
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
            if (e.VirtualKey == VirtualKey.Tab)
            {
                var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                var x = pointerPosition.X - Window.Current.Bounds.X;
                var y = pointerPosition.Y - Window.Current.Bounds.Y;
                var pos = new Point(x, y);
                var topCollection = VisualTreeHelper.FindElementsInHostCoordinates(pos, this).OfType<ICollectionView>().FirstOrDefault();

                // add tabitemviewmodels that directs user to documentviews within the current collection 
                var docViews = (topCollection as CollectionFreeformView).GetImmediateDescendantsOfType<DocumentView>();

                // TODO write a method called (AddItemToTabMenu) which takes in a view model, limit your publicly available variables
                // TODO when you have publicly accessible variables that are changed from anywhere you create spaghetti
                var tabItems = new List<ITabItemViewModel>(TabMenu.Instance.AllTabItems);
                // TODO why are we adding the document views when we press tab, are they goin to be added over and over again?
                // no because we make an entirely new list of them everytime apparently??

                /*
                foreach (DocumentView dv in docViews)
                {
                    tabItems.Add(new GoToTabItemViewModel("Get: " + dv.ViewModel.DisplayName, dv.Choose));
                }
                */

                TabMenu.Configure(topCollection as CollectionFreeformView, pos);
                TabMenu.ShowAt(xCanvas);
                TabMenu.Instance.SetTextBoxFocus();
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
            TabMenu.Configure(topCollection as CollectionFreeformView, pos); 
            TabMenu.ShowAt(xCanvas, true);
            TabMenu.Instance.SetTextBoxFocus();
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

        }

        public CollectionView GetMainCollectionView()
        {
            return _mainCollectionView ?? (_mainCollectionView = xMainDocView.GetFirstDescendantOfType<CollectionView>());
        }

        public void AddOperatorsFilter(ICollectionView collection, DragEventArgs e)
        {
            TabMenu.AddsToThisCollection = collection as CollectionFreeformView;
            if (xCanvas.Children.Contains(TabMenu.Instance)) return;
            xCanvas.Children.Add(TabMenu.Instance);
            Point absPos = e.GetPosition(Instance);
            Canvas.SetLeft(TabMenu.Instance, absPos.X);
            Canvas.SetTop(TabMenu.Instance, absPos.Y);
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
            menu.CreateAndRunInstantiationAnimation(true);
            xMenuCanvas.Content = menu;
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

        public void DisplayElement(UIElement elementToDisplay, Point upperLeft, UIElement fromCoordinateSystem)
        {
            //var dropPoint = fromCoordinateSystem.TransformToVisual(xCanvas).TransformPoint(upperLeft);
            var dropPoint = Util.PointTransformFromVisual(upperLeft, fromCoordinateSystem, xCanvas);
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

        private void DocumentTest_OnDragStarting(UIElement sender, DragStartingEventArgs e)
        {
            Action<ICollectionView, DragEventArgs> dropAction = Actions.AddDocuments;
            e.Data.Properties[RadialMenuView.RadialMenuDropKey] = dropAction;
        }

        private void CollectionTest_OnDragStarting(UIElement sender, DragStartingEventArgs e)
        {
            Action<ICollectionView, DragEventArgs> dropAction = Actions.AddCollectionTEST;
            e.Data.Properties[RadialMenuView.RadialMenuDropKey] = dropAction;
        }

        private void NotesTest_OnDragStarting(UIElement sender, DragStartingEventArgs e)
        {
            Action<ICollectionView, DragEventArgs> dropAction = Actions.AddNotes;
            e.Data.Properties[RadialMenuView.RadialMenuDropKey] = dropAction;
        }

        private void TestEnvOnButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            int numDocuments = 1000;
            int numFields = 50;

            var docs = new List<DocumentController>();
            for (int i = 0; i < numDocuments; ++i)
            {
                if (i % 20 == 0)
                {
                    Debug.WriteLine($"Generated {i} documents");
                }
                docs.Add(new XampleFields(numFields, TypeInfo.Text, i).Document);
            }

            var doc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.CollectionKey] = new ListController<DocumentController>(docs)
            }, DocumentType.DefaultType);

            var colBox = new CollectionBox(new DocumentReferenceController(doc.GetId(), KeyStore.CollectionKey), viewType: CollectionView.CollectionViewType.Grid).Document;
            doc.SetActiveLayout(colBox, true, false);
            DisplayDocument(doc);
        }

        private void UIElementTest(object sender, TappedRoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Grid g = new Grid
            {
                Name = "XTestGrid",
                ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(400) }, new ColumnDefinition { Width = new GridLength(400) } },
                Height = 900
            };
            var documentView = new DocumentView(new DocumentViewModel(new XampleFields(50, TypeInfo.Text).Document));
            Grid.SetColumn(documentView, 1);
            g.Children.Add(documentView);
            sw.Stop();
            Debug.WriteLine($"Phase 2 took {sw.ElapsedMilliseconds} ms");
            xCanvas.Children.Add(g);
        }

        /// <summary>
        /// Shows the right-hand docked document options menu. Slides it in with animation.
        /// </summary>
        public void ShowDocumentMenu()
        {
            slideOut.Begin();
        }

        /// <summary>
        /// Hides the right-hand docked document options menu. Slides it out with animation.
        /// </summary>
        public void HideDocumentMenu()
        {
            slideIn.Begin();
        }

        private void Border_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }
        private void DelegateTestOnTapped(object sender, TappedRoutedEventArgs e)
        {
            var protoNumbers = new Numbers("1").Document;
            protoNumbers.SetField(Numbers.Number4FieldKey, new NumberController(1), true);
            var protoLayout = protoNumbers.GetActiveLayout();
            protoLayout.SetField(KeyStore.PositionFieldKey, new PointController(0, 0), true);

            DisplayDocument(protoNumbers);

            Random r = new Random();
            for (int i = 0; i < 10; ++i)
            {
                var delNumbers = protoNumbers.MakeDelegate();
                //if (i != 4)
                delNumbers.SetField(Numbers.Number4FieldKey,
                    new NumberController(i + 2), true);
                delNumbers.SetField(Numbers.Number5FieldKey,
                    new NumberController(0), true);
                var delLayout = protoLayout.MakeDelegate();
                delLayout.SetField(KeyStore.PositionFieldKey, new PointController(400 + 200 * (i / 5), i % 5 * 200), true);
                delNumbers.SetActiveLayout(delLayout, true, false);

                DisplayDocument(delNumbers);
            }
        }

        private void DocPointerReferenceOnTapped(object sender, TappedRoutedEventArgs e)
        {
            var textKey = new KeyController("B81560E5-DEDA-43B5-822A-22255E0F6DF0", "Text");
            var innerDict = new Dictionary<KeyController, FieldControllerBase>
            {
                [textKey] = new TextController("Prototype text")
            };
            DocumentController innerProto = new DocumentController(innerDict, DocumentType.DefaultType);
            var dict = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.DataKey] = innerProto
            };
            var proto = new DocumentController(dict, DocumentType.DefaultType);

            var freeform = new FreeFormDocument(new List<DocumentController>
                {
                    new TextingBox(new PointerReferenceController(new DocumentReferenceController(proto.GetId(), KeyStore.DataKey), textKey)).Document
                },
                new Point(0, 0), new Size(400, 400)).Document;
            proto.SetActiveLayout(freeform, true, false);

            var del1 = proto.MakeDelegate();
            var delLayout = del1.GetActiveLayout().MakeDelegate();
            delLayout.SetField(KeyStore.PositionFieldKey, new PointController(0, 0), true);
            del1.SetActiveLayout(delLayout, true, false);

            var innerDelDict = new Dictionary<KeyController, FieldControllerBase>
            {
                [textKey] = new TextController("Delegate 1 text")
            };
            var innerDel1 = new DocumentController(innerDelDict, DocumentType.DefaultType);
            del1.SetField(KeyStore.DataKey, innerDel1, true);

            DisplayDocument(proto);
            DisplayDocument(del1);
        }

       
    }
}