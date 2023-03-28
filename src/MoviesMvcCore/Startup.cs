namespace MoviesMvcCore
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Neo4j.Driver;
    using Neo4jClient;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(GetDriver);
            services.AddSingleton(GetGraphClient);
            services.AddControllersWithViews();
        }

        private IDriver GetDriver(IServiceProvider provider)
        {
            //We're creating a logger here that the IDriver can use, that also hooks into the ASPNET logger
            var logger = new Neo4jAspNetCoreLogger(provider.GetService<ILogger<IDriver>>())
            {
                //LogLevel is pulled from the ASP NET default logging level
                Level = Enum.Parse<LogLevel>(Configuration["Logging:LogLevel:Default"]) 
            };

            //Setup our IDriver instance to be injected 
            var driver = GraphDatabase.Driver(
                Configuration["Neo4j:Host"],
                AuthTokens.Basic(
                    Configuration["Neo4j:User"],
                    Configuration["Neo4j:Pass"]),
                config => config.WithLogger(logger)
            );
            return driver;
        }

        private IGraphClient GetGraphClient(IServiceProvider provider)
        {
            //Create our IGraphClient instance.
            var client = new BoltGraphClient(Configuration["Neo4j:Host"], Configuration["Neo4j:User"], Configuration["Neo4j:Pass"]);
            
            //We have to connect - as this is fully async, we need to 'Wait()'
            client.ConnectAsync().Wait();

            return client;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}