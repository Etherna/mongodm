using Etherna.MongODM.AspNetCoreSample.Models;
using Etherna.MongODM.AspNetCoreSample.Persistence;
using Etherna.MongODM.Core.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Etherna.MongODM.AspNetCoreSample
{
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
            services.AddRazorPages();

            services.AddMongODMWithHangfire<ModelBase>()
                .AddDbContext<ISampleDbContext, SampleDbContext>();

            services.AddMongODMAdminDashboard();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            // Configure Asp.Net application.
            app.UseDeveloperExceptionPage();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthorization();

            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                Queues = new[]
                {
                    Queues.DB_MAINTENANCE,
                    "default"
                }
            });
            app.UseHangfireDashboard();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
