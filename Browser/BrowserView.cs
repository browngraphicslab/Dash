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
            set { _socket = value; }
        }

        private static DataWriter _dataMessageWriter;


        private static BrowserView _current;
        public static BrowserView Current
        {
            get { return _current; }
            private set
            {
                _current = value;
                CurrentTabChanged?.Invoke(value, value);
            }
        }

        public static event EventHandler<BrowserView> CurrentTabChanged;


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
            var array = read.CreateObjectList<BrowserRequest>();
            //array.ForEach(t => t.Handle(this.));
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

        public static BrowserView OpenTab(String url)
        {
            return new BrowserView(url);
        }

        private string _url;
        private double _scroll;
        private string Id = Guid.NewGuid().ToString("N");

        public event EventHandler<string> UrlChanged;
        public event EventHandler<double> ScrollChanged;

        public string Url { get { return _url; } }

        public BrowserView(string initialUrl = "http://www.google.com")
        {
            var r = new NewTabBrowserRequest();
            r.tabId = Id;
            r.url = initialUrl;
            SendToServer(r.Serialize());

        }

        public void FireUrlUpdated(string url)
        {
            _url = url;
            UrlChanged?.Invoke(this, url);
        }

        public void SetUrl(string url)
        {
            _url = url;
            var request = new SetUrlRequest();
            request.url = url;
            request.Send();
            UrlChanged?.Invoke(this, url);
        }

        public void FireScrollUpdated(double scroll)
        {
            _scroll = scroll;
            ScrollChanged?.Invoke(this, scroll);
        }

        public void SetScroll(double scroll)
        {
            _scroll = scroll;
            //var request = new SetUrlRequest();
            //request. = url;
            //request.Send();
            ScrollChanged?.Invoke(this, scroll);
        }
    }
}
