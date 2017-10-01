using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;
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
            serviceCollection.AddSingleton<IModelEndpoint<DocumentModel>, LocalDocumentEndpoint>();
            serviceCollection.AddSingleton<IModelEndpoint<FieldModel>, IModelEndpoint<FieldModel>>();
            serviceCollection.AddSingleton<IModelEndpoint<KeyModel>, IModelEndpoint<KeyModel>>();


            serviceCollection.AddTransient<IController<DocumentModel>, DocumentController>();


            // view model services, these are here because they rely on access to server controllers in their constructors
            serviceCollection.AddTransient<LoginViewModel>();

            // initialize the connection to the hub, no more proxies can be added after this line
            //hubConnection.Start().Wait();
            return serviceCollection.BuildServiceProvider();
        }

    }

    
}
