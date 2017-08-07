using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Swagger;

namespace DashWebServer
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // add in memory caching, this is used in the document repository so it must be added before the documentRepository
            services.AddMemoryCache();

            // Add a reference to the document repository
            services.AddSingleton<IDocumentRepository, CosmosDb>();

            // Add a reference to the real time server, this uses the document repository so it must be created after it
            services.AddSingleton(
                provider =>
                {
                    var db = provider.GetRequiredService<IDocumentRepository>();
                    var server = new RealtimeServer(db);
                    Task.Run(() => { server.Start(); });
                    return server;
                });

            // Add framework services.
            services.AddMvc();

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "My API",
                    Version = "v1",
                    Description = "The base API for the Dash App Produced by the Brown Graphics Lab",
                    TermsOfService = "None",
                    Contact = new Contact {Email = "luke_murray@brown.edu", Name = "Luke Murray"}
                });

                //Set the comments path for the swagger json and ui.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "DashWebServer.xml");
                c.IncludeXmlComments(xmlPath);
            });

            // Push
            services.AddWebSocketManager();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Information about logging and these settings can be found here
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging
            loggerFactory
                .WithFilter(new FilterLoggerSettings
                {
                    {"Microsoft", LogLevel.Warning},
                    {"System", LogLevel.Warning},
                    {"DashWebServer", LogLevel.Debug}
                })
                .AddConsole()
                .AddDebug()
                .AddFile("Logs/DashWebServer-{DATE}.txt", LogLevel.Information);

            app.UseMvcWithDefaultRoute();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"); });

            // Push
            app.UseWebSockets();
            app.MapWebSocketManager("/push", app.ApplicationServices.GetService<PushHandler>());

            app.UseStaticFiles();
        }
    }
}