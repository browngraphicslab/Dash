using System;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public partial class App
    {
        /// <summary>
        /// The container which we can use to get services which are registered in the <code>RegisterServices()</code> method
        /// You can use this container and access it anywhere using
        /// <para>
        /// <code>App.Instance.Container.GetRequiredService&ltDesiredService&gt;();</code>
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
            serviceCollection.AddSingleton<IModelEndpoint<FieldModel>, LocalSqliteEndpoint>();
            serviceCollection.AddSingleton<LocalPDFEndpoint, LocalPDFEndpoint>();
            return serviceCollection.BuildServiceProvider();
        }
    }

    
}
