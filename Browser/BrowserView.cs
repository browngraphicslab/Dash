using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Browser;
using DashShared;

namespace Dash
{
    public class BrowserView
    {
        private static bool _initted = false;
        private static bool _ready = false;

        public static event EventHandler<BrowserView> CurrentTabChanged;
        public static event EventHandler<BrowserView> NewTabCreated;
        private static readonly Dictionary<int, BrowserView> _browserViews = new Dictionary<int, BrowserView>();

        private static int _currentBrowserId = 0;
        public static BrowserView Current
        {
            get => GetBrowserView(_currentBrowserId);
            private set
            {
                _currentBrowserId = value.Id;
                CurrentTabChanged?.Invoke(value, value);
            }
        }

        /// <summary>
        /// Should only be called by server code
        /// </summary>
        /// <param name="browserId"></param>
        public static void UpdateCurrentFromServer(int browserId)
        {
            Current?.SetIsCurrent(false);

            Debug.Assert(_browserViews.ContainsKey(browserId));
            _browserViews[browserId].SetIsCurrent(true);

            Current = _browserViews[browserId];
        }

        public static BrowserView GetBrowserView(int browserId)
        {
            return _browserViews.ContainsKey(browserId) ? _browserViews[browserId] : null;
        }

        public static async Task HandleIncomingMessage(string read)
        {
            if (read.Equals("both"))
            {
                Debug.WriteLine("Connected to server and browser!");
                /*
                await UITask.RunTask(async () =>
                {
                    var size = await GetCurrentDisplaySize();
                    size = new Size(size.Width / 2, size.Height);
                    //ApplicationView.PreferredLaunchViewSize = size;
                    //ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
                    bool result = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryResizeView(size);
                });*/
            }
            else
            {
                var array = read.CreateObjectList<BrowserRequest>().ToArray();
                if (array.Length == 0)
                {
                    return;
                }
                foreach (var request in array.Where(t => !_browserViews.ContainsKey(t.tabId)))
                {
                    _browserViews.Add(request.tabId, new BrowserView(request.tabId));
                }

                foreach (var browserRequest in array)
                {
                    await browserRequest.Handle(_browserViews[browserRequest.tabId]);
                }
            }
        }

        public static async void SendToServer(BrowserRequest req)
        {
            await SendToServer(req.Serialize());
        }

        private static async Task SendToServer(string message)
        {
            try
            {

                await DotNetRPC.ChromeRequest(message);
            }
            catch (Exception e)
            {
                _initted = false;
                _ready = false;

                throw new Exception("Exception caught during writing to server data writer.  Reason: " + e.Message);
            }
        }

        public static void OpenTab(string url = "https://en.wikipedia.org/wiki/Special:RandomInCategory/Good_articles")
        {
            var r = new NewTabBrowserRequest {url = url};
            SendToServer(r.Serialize());
        }

        private readonly int Id;
        private long _startTimeOfBeingCurrent = 0;

        public event EventHandler<string> UrlChanged;
        public event EventHandler<double> ScrollChanged;
        public event EventHandler<bool> CurrentChanged;
        public event EventHandler<string> TitleChanged;
        public event EventHandler<string> ImageDataChanged;

        public string ImageData { get; private set; } = null;
        public double Scroll { get; private set; }

        public bool IsCurrent { get; private set; } = false;
        public string Url { get; private set; }

        public string Title { get; private set; }

        /// <summary>
        /// returns -1 if the tab isn't active
        /// </summary>
        public double MillisecondsSinceBecomingCurrentTab
        {
            get
            {
                if (!IsCurrent)
                {
                    return -1;
                }
                return (double)((DateTime.Now.Ticks - _startTimeOfBeingCurrent) / TimeSpan.TicksPerMillisecond);
            }
        }

        private BrowserView(int id)
        {
            Id = id;
            NewTabCreated?.Invoke(this, this);
        }

        public void FireUrlUpdated(string url)
        {
            Url = url;
            UrlChanged?.Invoke(this, url);
        }

        public void SetUrl(string url)
        {
            var request = new SetUrlRequest();
            request.tabId = Id;
            request.url = url;
            request.Send();
        }

        public void FireImageUpdated(string imageData)
        {
            Debug.WriteLine("Browser view image changed, with image different: " + imageData != ImageData);
            if (!string.IsNullOrEmpty(imageData))
            {
                Task.Run(SameImageTask);
            }
            ImageData = imageData;
            ImageDataChanged?.Invoke(this, imageData);

        }

        /// <summary>
        /// called to tell the browser to set the current Tab to this browser view
        /// </summary>
        public void MakeCurrent()
        {
            //TODO   
        }

        /// <summary>
        /// should only be called from browser request
        /// </summary>
        /// <param name="title"></param>
        public void FireTitleUpdated(string title)
        {
            Title = title;
            TitleChanged?.Invoke(this, title);
        }

        /// <summary>
        /// should only be called from browser request
        /// </summary>
        /// <param name="scroll"></param>
        public void FireScrollUpdated(double scroll)
        {
            Scroll = scroll;
            ScrollChanged?.Invoke(this, scroll);
        }

        public void SetScroll(double scroll)
        {
            //var request = new SetUrlRequest();
            //request.url = url;
            //request.Send();

            //tODO set scroll request

            var request = new SetScrollRequest();
            request.tabId = Id;
            request.scroll = scroll;
            request.Send();
        }

        private void SetIsCurrent(bool current)
        {
            IsCurrent = current;
            if (current)
            {
                _startTimeOfBeingCurrent = DateTime.Now.Ticks;
            }
            CurrentChanged?.Invoke(this, current);
        }

        public string GetUrlHash()
        {
            var hash = UtilShared.GetDeterministicGuid(Url).ToString();
            //Debug.WriteLine("Hash: "+hash+ "    url: "+ _url);
            return hash;
        }

        public DocumentContext GetAsContext()
        {
            return new DocumentContext()
            {
                Url = Url,
                Scroll = Scroll,
                Title = Title,
                ViewDuration = MillisecondsSinceBecomingCurrentTab,
                CreationTimeTicks = DateTime.Now.Ticks,
                ImageId = GetUrlHash()
            };
        }

        private Action SameImageTask = new Action(async () =>
        {
            var hash = MainPage.Instance.WebContext.GetUrlHash();
            if (MainPage.Instance.WebContext.ImageData == null || File.Exists(ApplicationData.Current.LocalFolder.Path + hash + ".jpg"))
            {
                return;
            }

            var prefix = "data:image/jpeg;base64,";

            byte[] byteBuffer = Convert.FromBase64String(MainPage.Instance.WebContext.ImageData.StartsWith(prefix)
                ? MainPage.Instance.WebContext.ImageData.Substring(prefix.Length)
                : MainPage.Instance.WebContext.ImageData);
            MemoryStream memoryStream = new MemoryStream(byteBuffer);
            memoryStream.Position = 0;

            await UITask.RunTask(async () =>
            {
                BitmapImage originalBitmap = new BitmapImage();
                await originalBitmap.SetSourceAsync(memoryStream.AsRandomAccessStream());

                memoryStream.Close();
                memoryStream = null;

                var height = originalBitmap.PixelHeight;
                var width = originalBitmap.PixelWidth;

                memoryStream = new MemoryStream(byteBuffer);

                var bitmapImage = new WriteableBitmap(width, height);
                await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());

                memoryStream.Close();
                memoryStream = null;
                byteBuffer = null;

                var util = new ImageToDashUtil();

                try
                {
                    await util.ParseBitmapAsync(bitmapImage, hash);
                }
                catch (Exception e)
                {

                }
            });
        });
    }
}
