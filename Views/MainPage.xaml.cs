using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
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
        public static MainPage Instance;
        private RadialMenuView _radialMenu;
        public static DocumentType MainDocumentType = new DocumentType("011EFC3F-5405-4A27-8689-C0F37AAB9B2E", "Main Document");
        private static CollectionView _mainCollectionView;

        public DocumentController MainDocument { get; private set; }

        public MainPage()
        {
            InitializeComponent();

            // create the collection document model using a request
            var fields = new Dictionary<Key, FieldModelController>();
            fields[DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(new List<DocumentController>());
            MainDocument = new DocumentController(fields, MainDocumentType);
            var collectionDocumentController =
                new CollectionBox(new ReferenceFieldModelController(MainDocument.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
            MainDocument.SetActiveLayout(collectionDocumentController, forceMask: true, addToLayoutList: true);

            // set the main view's datacontext to be the collection
            MainDocView.DataContext = new DocumentViewModel(MainDocument);

            // set the main view's width and height to avoid NaN errors
            MainDocView.Width = MyGrid.ActualWidth;
            MainDocView.Height = MyGrid.ActualHeight;

            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;

            //var jsonDoc = JsonToDashUtil.RunTests();

            //var sw = new Stopwatch();
            //sw.Start();
            //DisplayDocument(jsonDoc);
            //sw.Stop();

            _radialMenu = new RadialMenuView(xCanvas);
            xCanvas.Children.Add(_radialMenu);

            MainDocView.AllowDrop = true;
            MainDocView.DragEnter += MainDocViewOnDragEnter;
            MainDocView.Drop += MainDocView_Drop;
            MainDocView.DoubleTapped += XCanvas_OnDoubleTapped;
        }

        public CollectionView GetMainCollectionView()
        {
            return _mainCollectionView ?? (_mainCollectionView = MainDocView.GetFirstDescendantOfType<CollectionView>());
        }

        private void MainDocViewOnDragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            if (e.DragUIOverride == null) return;
            e.DragUIOverride.IsGlyphVisible = false;
            e.DragUIOverride.IsContentVisible = false;
            e.DragUIOverride.Caption = e.DataView.Properties.Title;
        }

        public void AddOperatorsFilter(object o, DragEventArgs e)
        {
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
            DBTest.DBDoc.AddChild(docModel);
        }

        private void MyGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainDocView.Width = e.NewSize.Width;
            MainDocView.Height = e.NewSize.Height;
        }

        //// FILE DRAG AND DROP

        /// <summary>
        ///     Handles drop events onto the canvas, usually by creating a copy document of the original and
        ///     placing it into the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">drag event arguments</param>
        private async void MainDocView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null)
            {
                (e.DataView.Properties[RadialMenuView.RadialMenuDropKey] as Action<object, DragEventArgs>)?.Invoke(sender, e);
                return;
            }

            var dragged = new Image();
            var url = "";

            
            // load items dragged from solution explorer
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                if (items.Any())
                {
                    var storageFile = items[0] as StorageFile;
                    var contentType = storageFile.ContentType;

                    var folder = ApplicationData.Current.LocalFolder;

                    // parse images dropped in
                    if (contentType == "image/jpg" || contentType == "image/png" || contentType == "image/jpeg")
                    {
                        var newFile = await storageFile.CopyAsync(folder, storageFile.Name,
                            NameCollisionOption.GenerateUniqueName);
                        url = newFile.Path;
                        var bitmapImg = new BitmapImage();

                        bitmapImg.SetSource(await storageFile.OpenAsync(FileAccessMode.Read));
                        dragged.Source = bitmapImg;
                    }

                    // parse text files dropped in
                    if (contentType == "text/plain")
                        return;
                }
            }

            if (e.DataView.Properties["image"] != null)
                dragged = e.DataView.Properties["image"] as Image; // fetches stored drag object

            // make document
            // generate single-image document model
            var m = new ImageFieldModelController(new Uri(url));
            var fields = new Dictionary<Key, FieldModelController>
            {
                [new Key("DRAGIMGF-1E74-4577-8ACC-0685111E451C", "image")] = m
            };

            var col = new DocumentController(fields, new DocumentType("dragimage", "dragimage"));
            DisplayDocument(col);
        }

        public void xCanvas_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        public void DisplayElement(UIElement elementToDisplay, Point upperLeft, UIElement fromCoordinateSystem)
        {
            var dropPoint = fromCoordinateSystem.TransformToVisual(xCanvas).TransformPoint(upperLeft);

            xCanvas.Children.Add(elementToDisplay);
            Canvas.SetLeft(elementToDisplay, dropPoint.X);
            Canvas.SetTop(elementToDisplay, dropPoint.Y);
        }

        private void XCanvas_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (!_radialMenu.IsVisible)
                _radialMenu.JumpToPosition(e.GetPosition(xCanvas).X, e.GetPosition(xCanvas).Y);
            //else _radialMenu.IsVisible = false;
            e.Handled = true;
        }
        #region Requests

        private enum HTTPRequestMethod
        {
            Get,
            Post
        }

        private async void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // authorization on the tiwtter api
            var twitterBase = "https://api.twitter.com";
            var twitterAuthEndpoint = twitterBase.AppendPathSegments("oauth2", "token");
            var twitterConsumerKey = "GSrTmog2xY7PWzxSjfGQDuKAH";
            var twitterConsumerSecret = "6QOcnCElbr4u80tiWspoGQTYryFyyRoXxMgiSZv4fq0Fox3dhV";
            var token = await OAuth2Authentication(twitterAuthEndpoint, twitterConsumerKey, twitterConsumerSecret);

            var userName = "realdonaldtrump";
            var tweetsByUserURL = twitterBase.AppendPathSegments("1.1", "statuses", "user_timeline.json").SetQueryParams(new { screen_name = userName });
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
            } else if (method == HTTPRequestMethod.Post)
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
    }
}