using DashShared;
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
        private Dictionary<string, IPushController> _controllers = new Dictionary<string, IPushController>();

        private PushClient()
        {
            _connection = new Connection();
            _connection.StartConnectionAsync("ws://localhost:8160").ConfigureAwait(true);
            _connection.On("receiveMessage", (arguments) =>
            {
                var message = arguments[0] as PushMessage;
                var id = message.Id;
                var model = message.Model as EntityBase;
                var controller = _controllers[id];

                switch(message.PushType)
                {
                    case PushType.Create:
                        AddModel(model);
                        break;
                    case PushType.Update:
                        controller.PushUpdate(model);
                        break;
                    case PushType.Delete:
                        controller.PushDelete();
                        break;
                }
            });
        }

        private void AddModel(EntityBase model)
        {
            ContentController.AddModel(model);

        }

        public void AddController(IPushController controller)
        {
            _controllers[controller.GetId()] = controller;
        }

        public void RemoveController(IPushController controller)
        {
            _controllers.Remove(controller.GetId());
        }
    }
}
