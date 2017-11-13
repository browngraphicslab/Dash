using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DashWebServer
{
    public class Snapshot
    {
        public bool IsAcknowledged { get; set; }

        public int Id
        {
            get => Convert.ToInt32(WorldState["id"]);
            set => WorldState["id"] = value;
        }

        public long Time
        {
            get => Convert.ToInt64(WorldState["time"]);
            set => WorldState["time"] = value;
        }

        public ConcurrentDictionary<string, object> WorldState = new ConcurrentDictionary<string, object>();

        public Snapshot Diff(Snapshot s)
        {
            var diff = new Dictionary<string, object>();
            foreach (var kv in WorldState)
            {
                if (s.WorldState.ContainsKey(kv.Key) && !kv.Value.Equals(s.WorldState[kv.Key]))
                {
                    diff[kv.Key] = kv.Value;
                }
            }
            

            return new Snapshot
            {
                IsAcknowledged = false,
                WorldState = new ConcurrentDictionary<string, object>(diff)
            };
        }

        public byte[] ToBytes()
        {
            string json = JsonConvert.SerializeObject(WorldState);
            return Encoding.ASCII.GetBytes(json);
        }
    }
}
