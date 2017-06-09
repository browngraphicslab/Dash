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
        }
    }
}
