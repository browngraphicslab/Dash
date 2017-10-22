using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace DashWebServer
{
    public class Program
    {
        public static void Main(string[] args)
        {

            // start the REST API
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
