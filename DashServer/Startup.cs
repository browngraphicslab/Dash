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
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            var hubConfiguration = ConfigureSingalR();

            app.MapSignalR("/" + DashConstants.SignalrBaseUrl, hubConfiguration);

        }

        public HubConfiguration ConfigureSingalR()
        {
            var hubConfiguration = new HubConfiguration();
            // detail error messages in client when we are developing locally, but not in production!
            hubConfiguration.EnableDetailedErrors = DashConstants.DEVELOP_LOCALLY;
            //TODO authentication must be required in production
            // require that any client who access the hub or is called from the hub is authenticated!
            //GlobalHost.HubPipeline.RequireAuthentication();

            var kernel = new StandardKernel();
            var resolver = new NinjectSignalRDependencyResolver(kernel);
            RegisterSignalRServices(kernel);

            hubConfiguration.Resolver = resolver;

            return hubConfiguration;
        }

        private void RegisterSignalRServices(StandardKernel kernel)
        {
            var documentRepository = GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IDocumentRepository)) as IDocumentRepository;
            Debug.Assert(documentRepository != null);
            kernel.Bind<IDocumentRepository>().ToConstant(documentRepository).InSingletonScope();
            kernel.Bind<IServerContractShapeHub>().To<ShapeHub>();
        }
    }
}
