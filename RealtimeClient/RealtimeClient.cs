using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Dash
{
    class RealtimeClient
    {
        public delegate void HandleUpdate(Dictionary<string, object> update);

        public event HandleUpdate OnReceivedUpdate;
        private UdpClient _client;
        public CommandHistory History { get; set; }
        private string _server;
        private Timer _tick;
        private Command _command;

        public void Start(string server)
        {
            _server = server;
            _client = new UdpClient(new IPEndPoint(IPAddress.Any, 27002));
            History = new CommandHistory(64);

            var body = new Dictionary<string, object>();
            var message = new Message(body);
            message.Type = "connect";
            var bytes = message.ToBytes();
            _client.SendAsync(bytes, bytes.Length, _server, 25565);

            _command = new Command()
            {
                Time = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Update = new Dictionary<string, object>()
            };
            _tick = new Timer(Tick, null, 0, 100);

            while (true)
            {
                var data = _client.ReceiveAsync().Result;
                Debug.WriteLine(Encoding.ASCII.GetString(data.Buffer));
                var m = Encoding.ASCII.GetString(data.Buffer);
                var b = JsonConvert.DeserializeObject(m, typeof(Dictionary<string, object>)) as Dictionary<string, object>;

                OnReceivedUpdate?.Invoke(b);

                var ackBody = new Dictionary<string, object>();
                ackBody["id"] = b["id"];
                var ack = new Message(ackBody);
                ack.Type = "ack";
                var ackBytes = ack.ToBytes();
                _client.SendAsync(ackBytes, ackBytes.Length, _server, 25565);
            }
        }

        private void Tick(Object o)
        {
            SendUpdate(_command);
            _command = new Command()
            {
                Time = _command.Time,
                Update = new Dictionary<string, object>()
            };
        }

        public void AddCommand(Command command)
        {
            History.Enqueue(command);
            foreach (var kv in command.Update)
            {
                _command.Update[kv.Key] = command.Update[kv.Key];
            }

            _command.Time = command.Time;
        }

        private void SendUpdate(Command command)
        {
            var message = new Message(command);
            message.Type = "update";
            var bytes = message.ToBytes();
            _client.SendAsync(bytes, bytes.Length, _server, 25565);
        }
    }
}
