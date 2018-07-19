using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OfficeInterop
{
    public class ChromeApp
    {
        private Task _listenerTask;
        private HttpListener _listener;

        private WebSocket _client;

        private Mutex _queueMutex = new Mutex(false);
        private List<byte[]> _queue = new List<byte[]>();

        public event Action<string> MessageReceived;

        public ChromeApp()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://127.0.0.1:12345/dash/chrome/");
        }

        public void Start()
        {
            _listenerTask = Task.Factory.StartNew(StartConnection);
        }

        private async void StartConnection()
        {
            //TODO This doesn't deal with dropped connection perfectly
            while (true)
            {
                _listener.Start();
                var context = _listener.GetContext();
                var wsContext = await context.AcceptWebSocketAsync(null);
                _client = wsContext.WebSocket;
                ProcessEvents();
            }
        }

        private void ProcessEvents()
        {
            Debug.Assert(_client.State == WebSocketState.Open);
            byte[] rbuffer = new byte[512];
            string receiveString = "";
            ArraySegment<byte> readBuffer = new ArraySegment<byte>(rbuffer);
            var receiveTask = _client.ReceiveAsync(readBuffer, CancellationToken.None);

            Task sendTask = null;

            while (_client.State == WebSocketState.Open)
            {
                _queueMutex.WaitOne();
                if (_queue.Count > 0)
                {
                    var wbytes = _queue[0];
                    if (sendTask == null || sendTask.IsCompleted)
                    {
                        sendTask = _client.SendAsync(new ArraySegment<byte>(wbytes), WebSocketMessageType.Text, true,
                            CancellationToken.None);
                    }
                    _queue.RemoveAt(0);
                }
                _queueMutex.ReleaseMutex();

                if (receiveTask.IsCompleted)
                {
                    var res = receiveTask.Result;
                    receiveString += Encoding.UTF8.GetString(readBuffer.Array, 0, res.Count);
                    if (res.EndOfMessage)
                    {
                        ProcessMessage(receiveString);
                        receiveString = "";
                    }

                    receiveTask = _client.ReceiveAsync(readBuffer, CancellationToken.None);
                }
            }
        }
        public void Send(byte[] data)
        {
            _queueMutex.WaitOne();
            _queue.Add(data);
            _queueMutex.ReleaseMutex();
        }


        private void ProcessMessage(string message)
        {
            MessageReceived?.Invoke(message);
        }
    }
}
