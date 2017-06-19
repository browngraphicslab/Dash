using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public partial class App
    {
        /// <summary>
        /// The container which we can use to get services which are registered in the <code>RegisterServices()</code> method
        /// You can use this container and access it anywhere using
        /// <para>
        /// <code>App.Instance.Container.GetRequiredService&lt;DesiredService&gt;();</code>
        /// </para>
        /// </summary>
        public IServiceProvider Container;


        /// <summary>
        /// Registers the services for dependency injection
        /// </summary>
        /// <returns></returns>
        private IServiceProvider RegisterServices()
        {
            var serviceCollection = new ServiceCollection();

            // server endpoints
            serviceCollection.AddSingleton<ServerEndpoint, ServerEndpoint>();
            serviceCollection.AddTransient<AccountEndpoint, AccountEndpoint>();
            serviceCollection.AddTransient<AuthenticationEndpoint, AuthenticationEndpoint>();
            serviceCollection.AddTransient<ShapeEndpoint, ShapeEndpoint>();
            serviceCollection.AddSingleton<DocumentEndpoint, DocumentEndpoint>(); //TODO change to transient
            serviceCollection.AddSingleton<TypeEndpoint, TypeEndpoint>(); //TODO change to transient
            serviceCollection.AddSingleton<KeyEndpoint, KeyEndpoint>(); //TODO change to transient

            // Examples to be removed
            serviceCollection.AddSingleton<ExampleApiSource, ExampleApiSource>(); //TODO remove this its an example
            serviceCollection.AddSingleton<PricePerSquareFootApi, PricePerSquareFootApi>(); //TODO remove this its an example


            // view model services, these are here because they rely on access to server controllers in their constructors
            serviceCollection.AddTransient<LoginViewModel>();


            //signalr services

            // create a connection to the hub
            var hubConnection = new HubConnection(DashConstants.ServerBaseUrl + DashConstants.SignalrBaseUrl, false);

            // add any proxies here
            var shapeProxy = new ShapeProxy(hubConnection);
            serviceCollection.AddSingleton<ShapeProxy>(shapeProxy);

            // initialize the connection to the hub, no more proxies can be added after this line
            //hubConnection.Start().Wait();
            return serviceCollection.BuildServiceProvider();
        }

    }
}
