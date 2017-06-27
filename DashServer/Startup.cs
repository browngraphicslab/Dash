using System.Diagnostics;
using System.Web.Http;
using DashServer.Hubs;
using DashShared;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Ninject;
using Owin;

[assembly: OwinStartup(typeof(DashServer.Startup))]

namespace DashServer
{
    public partial class Startup
    {
        /// <summary>
        /// Called when the app starts up, but called after the ninject dependency injection 
        /// in App_Start/NinjectWebCommon.cs
        /// </summary>
        /// <param name="app"></param>
        public void Configuration(IAppBuilder app)
        {
            // Configure the identity provider for authorization
            ConfigureAuth(app);

            // create the SignalR Hub configuration
            var hubConfiguration = ConfigureSingalR();

            // allow the app to map connections to signalR hubs
            app.MapSignalR("/" + DashConstants.SignalrBaseUrl, hubConfiguration);

        }

        /// <summary>
        /// Configures SignalR including providing its own Dependency Injector
        /// </summary>
        /// <returns></returns>
        public HubConfiguration ConfigureSingalR()
        {
            var hubConfiguration = new HubConfiguration();
            // detail error messages in client when we are developing locally, but not in production!
            hubConfiguration.EnableDetailedErrors = DashConstants.DEVELOP_LOCALLY;
            //TODO authentication must be required in production
            // require that any client who access the hub or is called from the hub is authenticated!
            //GlobalHost.HubPipeline.RequireAuthentication();

            // Create a ninject kernel and resolver for SignalR
            var kernel = new StandardKernel();
            var resolver = new NinjectSignalRDependencyResolver(kernel);
            RegisterSignalRServices(kernel);

            // set the dependency resolver for signalR hubs
            hubConfiguration.Resolver = resolver;

            return hubConfiguration;
        }

        /// <summary>
        /// Register any services we need to access in SignalR Hub constructors
        /// </summary>
        /// <param name="kernel"></param>
        private void RegisterSignalRServices(StandardKernel kernel)
        {
            // get a reference to the database
            var documentRepository = GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IDocumentRepository)) as IDocumentRepository;
            Debug.Assert(documentRepository != null);

            // register the database dependency
            kernel.Bind<IDocumentRepository>().ToConstant(documentRepository).InSingletonScope();

            // register any hubs
            kernel.Bind<IServerContractShapeHub>().To<ShapeHub>();
        }
    }
}
