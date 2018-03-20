using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Browser;
using DashShared;

namespace Dash
{
    public class BrowserView
    {
        private static bool _initted = false;
        private static bool _ready = false;
        private static MessageWebSocket _socket;
        public static MessageWebSocket Socket
        {
            get
            {
                if (_socket == null)
                {
                    InitSocket();
                }
                return _socket;
            }
            set => _socket = value;
        }

        private static DataWriter _dataMessageWriter;
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
            if (Current != null)
            {
                Current.SetIsCurrent(false);
            }

            Debug.Assert(_browserViews.ContainsKey(browserId));
            _browserViews[browserId].SetIsCurrent(true);

            Current = _browserViews[browserId];
        }

        public static BrowserView GetBrowserView(int browserId)
        {
            return _browserViews.ContainsKey(browserId) ? _browserViews[browserId] : null;
        }

        private static async Task InitSocket()
        {
            if (_initted)
            {
                return;
            }
            _initted = true;
            _socket = new MessageWebSocket();

            _socket.Control.MessageType = SocketMessageType.Utf8;
            _socket.Control.MaxMessageSize = UInt32.MaxValue;
            _socket.MessageReceived += MessageRecieved;
            _socket.Closed += SocketClosed;

            _dataMessageWriter = new DataWriter(_socket.OutputStream);

            await _socket.ConnectAsync(new Uri("ws://dashchromewebapp.azurewebsites.net/api/values"));
        }

        private static void SocketClosed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            _initted = false;
            _socket = null;
            _dataMessageWriter = null;
        }

        private static async void MessageRecieved(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    var bytes = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(bytes);
                    string read = Encoding.UTF8.GetString(bytes);
                    await HandleIncomingMessage(read);
                }
            }
            catch (Exception)
            {
                _initted = false;
                _socket = null;
                _dataMessageWriter = null;
                _ready = false;
                Debug.WriteLine("connection to server failed");
                Debug.WriteLine("communication will be cut until connection resumes");
                //throw new Exception("connection to server failed");
            }
        }


        /*
        private static async Task<Size> GetCurrentDisplaySize()
        {
            Size s = new Size();
            await UITask.RunTask(async () =>
            {
                var displayInformation = DisplayInformation.GetForCurrentView();
                System.Reflection.TypeInfo t = typeof(DisplayInformation).GetTypeInfo();
                var props = t.DeclaredProperties
                    .Where(x => x.Name.StartsWith("Screen") && x.Name.EndsWith("InRawPixels")).ToArray();
                var w = props.Where(x => x.Name.Contains("Width")).First().GetValue(displayInformation);
                var h = props.Where(x => x.Name.Contains("Height")).First().GetValue(displayInformation);
                var size = new Size(System.Convert.ToDouble(w), System.Convert.ToDouble(h));
                switch (displayInformation.CurrentOrientation)
                {
                    case DisplayOrientations.Landscape:
                    case DisplayOrientations.LandscapeFlipped:
                        size = new Size(Math.Max(size.Width, size.Height), Math.Min(size.Width, size.Height));
                        break;
                    case DisplayOrientations.Portrait:
                    case DisplayOrientations.PortraitFlipped:
                        size = new Size(Math.Min(size.Width, size.Height), Math.Max(size.Width, size.Height));
                        break;
                }
                s = size;
            });
            return s;
        }
        */

        private static string _prevPartialString = "";
        private static async Task HandleIncomingMessage(string read)
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
                if (!string.IsNullOrWhiteSpace(_prevPartialString) && !read.EndsWith(']'))
                {
                    _prevPartialString += read;
                    if (_prevPartialString.Length > 25000000)
                    {
                        _prevPartialString = "";
                    }
                }
                else if (read.Contains("{") || read.Length > 100)
                {
                    var array = (_prevPartialString + read).CreateObjectList<BrowserRequest>().ToArray();
                    if (array.Count() == 0)
                    {
                        _prevPartialString += read;
                        if (_prevPartialString.Length > 25000000)
                        {
                            _prevPartialString = "";
                        }
                        return;
                    }
                    else
                    {
                        _prevPartialString = "";
                    }
                    foreach (var request in array.Where(t => !_browserViews.ContainsKey(t.tabId)))
                    {
                        var browser = new BrowserView(request.tabId);
                    }
                    array.ToList().ForEach(t => t.Handle(_browserViews[t.tabId]));
                }
            }
        }

        public static async void SendToServer(BrowserRequest req)
        {
            await SendToServer(req.Serialize());
        }

        private static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                throw new Exception("No network adapters with an IPv4 address in the system!");
            }
            catch (Exception e)
            {
                return "123";
            }
        }

        private static async Task SendToServer(string message)
        {
            if (_socket == null)
            {
                await InitSocket();
                var ip = "123";
                _dataMessageWriter.WriteString("dash:"+ ip);
                await _dataMessageWriter.StoreAsync();
                _ready = true;
            }

            while (!_ready)
            {
                Debug.WriteLine("Awaiting connection to web server");
                await Task.Delay(50);
            }

            try
            {
                _dataMessageWriter.WriteString(message);
                await _dataMessageWriter.StoreAsync();
            }
            catch (Exception e)
            {
                _initted = false;
                _socket = null;
                _dataMessageWriter = null;
                _ready = false;

                throw new Exception("Exception caught during writing to server data writer.  Reason: " + e.Message);
            }
        }

        public static void ForceInit()
        {
            var r = new PingBrowserRequest();
            SendToServer(r.Serialize());
        }


        public static void OpenTab(string url = "https://en.wikipedia.org/wiki/Special:RandomInCategory/Good_articles")
        {
            var r = new NewTabBrowserRequest();
            r.url = url;
            SendToServer(r.Serialize());
        }

        private string _url;
        private double _scroll;
        private bool _isCurrent = false;
        private readonly int Id;
        private string _title;
        private long _startTimeOfBeingCurrent = 0;
        private string _imageData = null;

        public event EventHandler<string> UrlChanged;
        public event EventHandler<double> ScrollChanged;
        public event EventHandler<bool> CurrentChanged;
        public event EventHandler<string> TitleChanged;
        public event EventHandler<string> ImageDataChanged;

        public string ImageData => _imageData; 
        public double Scroll => _scroll;
        public bool IsCurrent => _isCurrent;
        public string Url => _url;
        public string Title => _title;

        /// <summary>
        /// returns -1 if the tab isn't active
        /// </summary>
        public double MillisecondsSinceBecomingCurrentTab
        {
            get
            {
                if (!_isCurrent)
                {
                    return -1;
                }
                return (double) ((DateTime.Now.Ticks - _startTimeOfBeingCurrent) / TimeSpan.TicksPerMillisecond);
            }
        }

        private BrowserView(int id)
        {
            Id = id;
            _browserViews.Add(id, this);
            NewTabCreated?.Invoke(this,this);
        }

        public void FireUrlUpdated(string url)
        {
            _url = url;
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
            Debug.WriteLine("Browser view image changed, with image different: "+ imageData != _imageData);
            if (!string.IsNullOrEmpty(imageData))
            {
                Task.Run(SameImageTask);
            }
            _imageData = imageData;
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
            _title = title;
            TitleChanged?.Invoke(this, title);
        }

        /// <summary>
        /// should only be called from browser request
        /// </summary>
        /// <param name="scroll"></param>
        public void FireScrollUpdated(double scroll)
        {
            _scroll = scroll;
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
            _isCurrent = current;
            if (current)
            {
                _startTimeOfBeingCurrent = DateTime.Now.Ticks;
            }
            CurrentChanged?.Invoke(this, current);
        }

        public string GetUrlHash()
        {
            var hash =  UtilShared.GetDeterministicGuid(_url);
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
