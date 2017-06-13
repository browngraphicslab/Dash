using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            // server and server controller related services
            serviceCollection.AddSingleton<ServerController, ServerController>();
            serviceCollection.AddTransient<AccountController, AccountController>();
            serviceCollection.AddTransient<AuthenticationController, AuthenticationController>();
            serviceCollection.AddSingleton<DocumentController, DocumentController>(); //TODO change to transient
            serviceCollection.AddSingleton<TypeController, TypeController>(); //TODO change to transient
            serviceCollection.AddSingleton<KeyController, KeyController>(); //TODO change to transient


            // view model services, these are here because they rely on access to server controllers in their constructors
            serviceCollection.AddTransient<LoginViewModel>();


            return serviceCollection.BuildServiceProvider();
        }

    }
}
