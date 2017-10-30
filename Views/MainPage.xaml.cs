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

        public RadialMenuView RadialMenu => _radialMenu;
        public DocumentController MainDocument { get; private set; }

        public MainPage()
        {

            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = ((SolidColorBrush)Application.Current.Resources["DocumentBackground"]).Color;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            InitializeComponent();

            // create the collection document model using a request
            var fields = new Dictionary<KeyController, FieldControllerBase>();
            fields[KeyStore.CollectionKey] = new DocumentCollectionFieldModelController(new List<DocumentController>());
            MainDocument = new DocumentController(fields, DashConstants.TypeStore.MainDocumentType);
            
            var collectionDocumentController =
                new CollectionBox(new DocumentReferenceFieldController(MainDocument.GetId(), KeyStore.CollectionKey), w: MyGrid.ActualWidth, h: MyGrid.ActualHeight).Document;
            //collectionDocumentController.SetField(CourtesyDocument.HorizontalAlignmentKey, new TextFieldModelController(HorizontalAlignment.Stretch.ToString()), true);
            //collectionDocumentController.SetField(CourtesyDocument.VerticalAlignmentKey, new TextFieldModelController(VerticalAlignment.Stretch.ToString()), true);
            //collectionDocumentController.SetField(CourtesyDocument.VerticalAlignmentKey, new TextFieldModelController(VerticalAlignment.Stretch.ToString()), true);
            MainDocument.SetActiveLayout(collectionDocumentController, forceMask: true, addToLayoutList: true);

            // set the main view's datacontext to be the collection
            xMainDocView.DataContext = new DocumentViewModel(MainDocument);

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
                    MainDocument = ContentController<DocumentModel>.GetController<DocumentController>(doc.Id);

                    var layout = new CollectionBox(new DocumentReferenceFieldController(MainDocument.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
                    MainDocument.SetActiveLayout(layout, true, true);
                }
                else
                {
                    var fields = new Dictionary<KeyController, FieldControllerBase>
                    {
                        [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController()
                    };
                    MainDocument = new DocumentController(fields, DashConstants.TypeStore.MainDocumentType);

                    var layout = new CollectionBox(new DocumentReferenceFieldController(MainDocument.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
                    MainDocument.SetActiveLayout(layout, true, true);
                }
                xMainDocView.DataContext = new DocumentViewModel(MainDocument);
            }

            await RESTClient.Instance.Documents.GetDocumentsByQuery<DocumentModel>(
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
                TabMenu.ConfigureAndShow(topCollection as CollectionFreeformView, pos, xCanvas);
                TabMenu.Instance?.AddGoToTabItems(docViews.ToList());
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
            var children = MainDocument.GetDereferencedField(KeyStore.CollectionKey, null) as DocumentCollectionFieldModelController;
            DBTest.ResetCycleDetection();
            children?.AddDocument(docModel);
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
                [KeyStore.CollectionKey] = new DocumentCollectionFieldModelController(docs)
            }, DocumentType.DefaultType);

            var colBox = new CollectionBox(new DocumentReferenceFieldController(doc.GetId(), KeyStore.CollectionKey), viewType: CollectionView.CollectionViewType.Grid).Document;
            doc.SetActiveLayout(colBox, true, false);
            DisplayDocument(doc);
        }

        private void UIElementTest(object sender, TappedRoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //var sp = new StackPanel
            //{
            //    Orientation = Orientation.Vertical,
            //    Width = 400,
            //    Height = 1000
            //};
            Grid g = new Grid
            {
                Name = "XTestGrid",
                ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(400) }, new ColumnDefinition { Width = new GridLength(400) } },
                Height = 900
            };
            //List<FrameworkElement> elements = new List<FrameworkElement>();
            //GridView gv = new GridView();
            //Canvas.SetLeft(g, 200);
            //Grid.SetColumn(gv, 0);
            //for (int i = 0; i < 50; ++i)
            //{
            //    var tb = new EditableTextBlock();
            //    TextingBox.SetupBindings(tb, new TextingBox(new TextFieldModelController("Test " + i)).Document, new Context());
            //    //sp.Children.Add(tb);
            //    elements.Add(tb);
            //}
            //gv.ItemsSource = elements;
            //g.Children.Add(gv);
            //sw.Stop();
            //Debug.WriteLine($"Phase 1 took {sw.ElapsedMilliseconds} ms");
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
            protoNumbers.SetField(Numbers.Number4FieldKey, new NumberFieldModelController(1), true);
            var protoLayout = protoNumbers.GetActiveLayout().Data;
            protoLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(0, 0), true);

            DisplayDocument(protoNumbers);

            Random r = new Random();
            for (int i = 0; i < 10; ++i)
            {
                var delNumbers = protoNumbers.MakeDelegate();
                //if (i != 4)
                delNumbers.SetField(Numbers.Number4FieldKey,
                    new NumberFieldModelController(i + 2), true);
                delNumbers.SetField(Numbers.Number5FieldKey,
                    new NumberFieldModelController(0), true);
                var delLayout = protoLayout.MakeDelegate();
                delLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(400 + 200 * (i / 5), i % 5 * 200), true);
                delNumbers.SetActiveLayout(delLayout, true, false);

                DisplayDocument(delNumbers);
            }
        }

        private void DocPointerReferenceOnTapped(object sender, TappedRoutedEventArgs e)
        {
            var textKey = new KeyController("B81560E5-DEDA-43B5-822A-22255E0F6DF0", "Text");
            var innerDict = new Dictionary<KeyController, FieldControllerBase>
            {
                [textKey] = new TextFieldModelController("Prototype text")
            };
            DocumentController innerProto = new DocumentController(innerDict, DocumentType.DefaultType);
            var dict = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.DataKey] = new DocumentFieldModelController(innerProto)
            };
            var proto = new DocumentController(dict, DocumentType.DefaultType);

            var freeform = new FreeFormDocument(new List<DocumentController>
                {
                    new TextingBox(new PointerReferenceFieldController(new DocumentReferenceFieldController(proto.GetId(), KeyStore.DataKey), textKey)).Document
                },
                new Point(0, 0), new Size(400, 400)).Document;
            proto.SetActiveLayout(freeform, true, false);

            var del1 = proto.MakeDelegate();
            var delLayout = del1.GetActiveLayout().Data.MakeDelegate();
            delLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(0, 0), true);
            del1.SetActiveLayout(delLayout, true, false);

            var innerDelDict = new Dictionary<KeyController, FieldControllerBase>
            {
                [textKey] = new TextFieldModelController("Delegate 1 text")
            };
            var innerDel1 = new DocumentController(innerDelDict, DocumentType.DefaultType);
            del1.SetField(KeyStore.DataKey, new DocumentFieldModelController(innerDel1), true);

            DisplayDocument(proto);
            DisplayDocument(del1);
        }

       
    }
}