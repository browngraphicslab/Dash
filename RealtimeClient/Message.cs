using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using Newtonsoft.Json;

namespace Dash
{
    public class Message
    {
        public Dictionary<string, object> Body = new Dictionary<string, object>();
        public string Type {
            get => Body["type"] as string;
            set => Body["type"] = value;
        }
        public readonly IPEndPoint Sender;

        private Message(byte[] data)
        {
            if (data.Length == 0)
            {
                throw new ArgumentException("Result buffer cannot be of length zero.");
            }

            var message = Encoding.ASCII.GetString(data);
            Body = JsonConvert.DeserializeObject(message) as Dictionary<string, object>;
        }

        public Message(UdpReceiveResult result) : this(result.Buffer)
        {
            Sender = result.RemoteEndPoint;
        }

        public Message(Dictionary<string, object> diff)
        {
            Body = new Dictionary<string, object>(diff);
        }

        public Message(Command command) : this(command.Update)
        {
             Body["time"] = command.Time;
        }

        public byte[] ToBytes()
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(Body));
        }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(Body)}: {Body}, {nameof(Sender)}: {Sender}";
        }
    }
}
