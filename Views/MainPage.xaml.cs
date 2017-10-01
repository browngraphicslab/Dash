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
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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


        public DocumentController MainDocument { get; private set; }

        public static InkFieldModelController InkFieldModelController = new InkFieldModelController();

        public MainPage()
        {
            InitializeComponent();

            _radialMenu = new RadialMenuView(xCanvas);
            xCanvas.Children.Add(_radialMenu);

            var matrix = new Matrix3x2(1, 0, 0, 1, 1, 1);
            Debug.WriteLine("Translate + 10, 10: " + Matrix3x2.CreateTranslation(10, 10));
            Debug.WriteLine("Scale 10, 10: " + Matrix3x2.CreateScale(10, 10));
            TestMatrix(2, 2, 0, 0, 1, 1);
            TestMatrix(2, 2, 1, 1, 1, 1);
            TestMatrix(2, 2, 2, 2, 1, 1);
            TestMatrix(2, 2, 4, 4, 1, 1);
            TestMatrix(4, 4, 0, 0, 2, 2);
            TestMatrix(4, 4, 1, 1, 2, 2);
            TestMatrix(4, 4, 2, 2, 2, 2);
            TestMatrix(4, 4, 4, 4, 2, 2);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is DocumentController)
            {
                // get the main document from the navigation context
                MainDocument = e.Parameter as DocumentController;
                // set the main view's datacontext to be the collection
                xMainDocView.DataContext = new DocumentViewModel(MainDocument);

                // set the main view's width and height to avoid NaN errors
                xMainDocView.Width = MyGrid.ActualWidth;
                xMainDocView.Height = MyGrid.ActualHeight;

                // Set the instance to be itself, there should only ever be one MainView
                Debug.Assert(Instance == null,
                    "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
                Instance = this;

                //var jsonDoc = JsonToDashUtil.RunTests();
            }
        }

        private void TestMatrix(float xScale, float yScale, float xCenter, float yCenter, float translateX, float translateY)
        {
            //var matrix = Matrix3x2.CreateScale(xScale, yScale, new Vector2(xCenter, yCenter));
            //Debug.WriteLine("Scale " + xScale + ", " + yScale + " with center " + xCenter + ", " + yCenter + ": ");
            //Debug.WriteLine("|" + matrix.M11 + " " + matrix.M12 + "|");
            //Debug.WriteLine("|" + matrix.M21 + " " + matrix.M22 + "|");
            //Debug.WriteLine("|" + matrix.M31 + " " + matrix.M32 + "|");
        }

        public CollectionView GetMainCollectionView()
        {
            return _mainCollectionView ?? (_mainCollectionView = xMainDocView.GetFirstDescendantOfType<CollectionView>());
        }

        public void AddOperatorsFilter(ICollectionView collection, DragEventArgs e)
        {
            OperatorSearchView.AddsToThisCollection = collection as CollectionFreeformView;
            if (xCanvas.Children.Contains(OperatorSearchView.Instance)) return;
            xCanvas.Children.Add(OperatorSearchView.Instance);
            Point absPos = e.GetPosition(Instance);
            Canvas.SetLeft(OperatorSearchView.Instance, absPos.X);
            Canvas.SetTop(OperatorSearchView.Instance, absPos.Y);
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
            var children = MainDocument.GetDereferencedField(DocumentCollectionFieldModelController.CollectionKey, null) as DocumentCollectionFieldModelController;
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

            var responseAsDocument = JsonToDashUtil.Parse(tweetsByUser, tweetsByUserURL.ToString(true));
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
                [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(docs)
            }, DocumentType.DefaultType);

            var colBox = new CollectionBox(new DocumentReferenceFieldController(doc.GetId(), DocumentCollectionFieldModelController.CollectionKey), viewType: CollectionView.CollectionViewType.Grid).Document;
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

        private void DelegateTestOnTapped(object sender, TappedRoutedEventArgs e)
        {
            var protoNumbers = new Numbers().Document;
            protoNumbers.SetField(Numbers.Number4FieldKey, new NumberFieldModelController(1), true);
            var protoLayout = protoNumbers.GetActiveLayout().Data;
            protoLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(0, 0), true);

            DisplayDocument(protoNumbers);

            Random r = new Random();
            for (int i = 0; i < 10; ++i)
            {
                var delNumbers = protoNumbers.MakeDelegate();
                if (i != 4)
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
    }
}