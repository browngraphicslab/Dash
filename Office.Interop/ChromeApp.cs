using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OfficeInterop
{
    public class ChromeApp
    {
        private Task _listenerTask;

        private TcpClient _client;
        private TcpListener _listener;

        private Mutex _queueMutex = new Mutex(false);
        private List<byte[]> _queue = new List<byte[]>();

        public event Action<string> MessageReceived;

        public ChromeApp()
        {
            _client = new TcpClient();
            _listener = new TcpListener(IPAddress.Loopback, 12345);
        }

        public void Start()
        {
            _listenerTask = Task.Factory.StartNew(StartConnection);
        }

        private void StartConnection()
        {
            //TODO This doesn't deal with dropped connection perfectly
            while (true)
            {
                if (!_client.ConnectAsync(IPAddress.Loopback, 54321).Wait(100))
                {
                    _listener.Start();
                    _client = _listener.AcceptTcpClient();
                }
                ProcessEvents();
            }
        }

        private void ProcessEvents()
        {
            var stream = _client.GetStream();
            stream.ReadTimeout = 100;
            stream.WriteTimeout = 100;
            byte[] buffer = new byte[512];
            while (_client.Connected)
            {

                _queueMutex.WaitOne();
                foreach (var bytese in _queue)
                {
                    stream.Write(bytese, 0, bytese.Length);
                }
                _queueMutex.ReleaseMutex();

                List<byte> bytes = new List<byte>();
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; ++i)
                    {
                        bytes.Add(bytes[i]);
                    }
                }

                if (bytes.Count > 0)
                {
                    ProcessMessage(Encoding.UTF8.GetString(bytes.ToArray()));
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
