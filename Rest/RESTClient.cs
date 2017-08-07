using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class RESTClient
    {
        private static readonly Lazy<RESTClient> lazy =
        new Lazy<RESTClient>(() => new RESTClient());

        public static RESTClient Instance { get { return lazy.Value; } }
        private ServerEndpoint _server;
        public KeyEndpoint Keys { get; set; }
        public FieldEndpoint Fields { get; set; }
        public DocumentEndpoint Documents { get; set; }

        private RESTClient()
        {
            _server = new ServerEndpoint();
            Keys = new KeyEndpoint(_server);
            Fields = new FieldEndpoint(_server);
            Documents = new DocumentEndpoint(_server);
        }
    }
}
