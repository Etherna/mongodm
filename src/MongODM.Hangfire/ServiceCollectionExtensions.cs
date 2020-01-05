using Digicando.MongODM.HF.Filters;
using Digicando.MongODM.HF.Tasks;
using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Utility;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Digicando.MongODM.HF
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMongoDMHangfire<TDbContext, TDbContextImpl, TProxyGenerator>(this IServiceCollection services)
            where TDbContext : class, IDbContext
            where TDbContextImpl : class, TDbContext
            where TProxyGenerator: class, IProxyGenerator
        {
            services.AddMongoDM<TDbContext, TDbContextImpl, TProxyGenerator, TaskRunner>();

            // Add Hangfire filters.
            GlobalJobFilters.Filters.Add(new LocalContextFilter(AsyncLocalContextAccessor.Instance));
        }
    }
}
