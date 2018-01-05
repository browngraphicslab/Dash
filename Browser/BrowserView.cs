using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
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
            
        }

        private static async void MessageRecieved(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    string read = reader.ReadString(reader.UnconsumedBufferLength);
                    await HandleIncomingMessage(read);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("connection to server failed");
                throw new Exception("connection to server failed");
            }
        }

        private static async Task HandleIncomingMessage(string read)
        {
            if (read.Equals("both"))
            {
                Debug.WriteLine("Connected to server and browser!");
            }
            else
            {
                if (read.Contains("{"))
                {
                    var array = read.CreateObjectList<BrowserRequest>();
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
        private static async Task SendToServer(string message)
        {
            if (_socket == null)
            {
                await InitSocket();
                _dataMessageWriter.WriteString("dash:123");
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
                throw new Exception("Exception caught during writing to server data writer.  Reason: " + e.Message);
            }
        }

        public static void OpenTab(string url)
        {
            var r = new NewTabBrowserRequest();
            r.url = url;
            SendToServer(r.Serialize());
            var a = r.Serialize();
        }

        private string _url;
        private double _scroll;
        private bool _isCurrent = false;
        private readonly int Id;

        public double Scroll => _scroll;
        public bool IsCurrent => _isCurrent;
        public event EventHandler<string> UrlChanged;
        public event EventHandler<double> ScrollChanged;
        public event EventHandler<bool> CurrentChanged;

        public string Url => _url;

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

        /// <summary>
        /// called to tell the browser to set the current Tab to this browser view
        /// </summary>
        public void MakeCurrent()
        {
            //TODO   
        }

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
        }

        private void SetIsCurrent(bool current)
        {
            _isCurrent = current;
            CurrentChanged?.Invoke(this, current);
        }
    }
}
