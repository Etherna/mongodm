using Digicando.MongoDM.HF.Filters;
using Digicando.MongoDM.HF.Tasks;
using Digicando.MongoDM.ProxyModels;
using Digicando.MongoDM.Utility;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Digicando.MongoDM.HF
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
