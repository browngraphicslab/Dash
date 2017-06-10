using DashShared;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(DashServer.Startup))]

namespace DashServer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);


            var hubConfiguration = new HubConfiguration();
            // detail error messages in client when we are developing locally, but not in production!
            hubConfiguration.EnableDetailedErrors = DashConstants.DEVELOP_LOCALLY;
            // require that any client who access the hub or is called from the hub is authenticated!
            GlobalHost.HubPipeline.RequireAuthentication();


            app.MapSignalR("/" + DashConstants.SignalrBaseUrl, hubConfiguration);
        }
    }
}
