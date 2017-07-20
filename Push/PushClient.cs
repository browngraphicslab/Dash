using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Push
{
    class PushClient
    {
        private static readonly Lazy<PushClient> lazy =
        new Lazy<PushClient>(() => new PushClient());

        public static PushClient Instance { get { return lazy.Value; } }

        private readonly Connection _connection;
        private Dictionary<string, IController> controllers = new Dictionary<string, IController>();

        public PushClient()
        {
            _connection = new Connection();
            _connection.StartConnectionAsync("ws://localhost:8160");
            _connection.On("receiveMessage", (arguments) =>
            {
                   
            });
        }

        public void AddController(IPushController controller)
        {

        }

        public void RemoveController(IPushController controller)
        {

        }
    }
}
