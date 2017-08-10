using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DashWebServer
{
    public class RealtimeServer
    {
        private static readonly int MAX_SNAPSHOTS = 32;
        ConcurrentDictionary<IPEndPoint, ClientSnapshots> clients =
            new ConcurrentDictionary<IPEndPoint, ClientSnapshots>();
        private UdpClient _udpServer;
        private Timer _tick;
        public ConcurrentDictionary<string, object> WorldState = new ConcurrentDictionary<string, object>();
        public ConcurrentDictionary<string, object> StartWorldState;
        public ConcurrentDictionary<IPEndPoint, long> LastCommand = new ConcurrentDictionary<IPEndPoint, long>();
        private IDocumentRepository db;

        public RealtimeServer(IDocumentRepository db)
        {
            this.db = db;
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            var hostName = Dns.GetHostName();
            _udpServer = new UdpClient(new IPEndPoint(IPAddress.Any, 25565));
            Debug.WriteLine(_udpServer.Client.LocalEndPoint);
            _tick = new Timer(Tick, null, 0, 50);
            WorldState["x"] = 0.0;
            WorldState["y"] = 0.0;
            StartWorldState = new ConcurrentDictionary<string, object>(WorldState);

            AcceptClients();
        }

        private void Tick(Object o)
        {
            //Debug.WriteLine("Tick");
            foreach (var kv in clients)
            {
                var address = kv.Key;
                var snapshots = kv.Value;

                var globalSnapshot = new Snapshot
                {
                    IsAcknowledged = false,
                    WorldState = new ConcurrentDictionary<string, object>(WorldState)
                };

                snapshots.Enqueue(globalSnapshot);

                var lastAcked = snapshots.LastAcknowledgedSnapshot() ?? new Snapshot
                {
                    IsAcknowledged = false,
                    WorldState = new ConcurrentDictionary<string, object>(StartWorldState)
                };

                var diff = globalSnapshot.Diff(lastAcked);
                Snapshot last = snapshots.Last();
                diff.Id = last.Id;
                diff.Time = LastCommand[address];
                SendUpdate(address, diff);
            }
        }

        private void SendUpdate(IPEndPoint address, Snapshot diff)
        {
            var bytes = new RealtimeMessage(diff)
            {
                Type = "update"
            }.ToBytes();

            try
            {
                _udpServer.SendAsync(bytes, bytes.Length, address);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private async void AcceptClients()
        {
            try
            {
                while (true)
                {
                    var result = await _udpServer.ReceiveAsync();
                    var message = new RealtimeMessage(result);
                    //Debug.WriteLine(message);

                    HandleMessage(message);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void HandleMessage(object o)
        {
            var message = o as RealtimeMessage;
            switch (message.Type)
            {
                case "connect":
                    clients.TryRemove(message.Sender, out _);
                    clients.TryAdd(message.Sender, new ClientSnapshots(MAX_SNAPSHOTS));
                    LastCommand[message.Sender] = 0;
                    break;
                case "update":
                    HandleUpdate(message.Sender, message.Body);
                    break;
                case "ack":
                    HandleAck(message);
                    break;
                case "none":
                    break;
            }
        }

        private void HandleUpdate(IPEndPoint client, Dictionary<string, object> args)
        {
            long sequence = Convert.ToInt64(args["time"]);
            LastCommand[client] = sequence;

            args.Remove("time");
            args.Remove("type");
            foreach (var kv in args)
            {
                WorldState[kv.Key] = Convert.ToDouble(kv.Value as double?);
            }
        }

        private void HandleAck(RealtimeMessage message)
        {
            var snapshots = clients[message.Sender];
            var id = Convert.ToInt32(message.Body["id"]);
            foreach (var snapshot in snapshots.Reverse())
            {
                if (snapshot.Id == id)
                {
                    snapshot.IsAcknowledged = true;
                    break;
                }
            }
        }
    }
}
