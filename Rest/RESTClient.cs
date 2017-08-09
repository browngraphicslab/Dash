using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class RESTClient
    {

        private static readonly Lazy<RESTClient> lazy = new Lazy<RESTClient>(() => new RESTClient());

        public static RESTClient Instance { get { return lazy.Value; } }

        public KeyEndpoint Keys => App.Instance.Container.GetRequiredService<KeyEndpoint>();
        public FieldEndpoint Fields => App.Instance.Container.GetRequiredService<FieldEndpoint>();   
        public DocumentEndpoint Documents => App.Instance.Container.GetRequiredService<DocumentEndpoint>();
    }
}
