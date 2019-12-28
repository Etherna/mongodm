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
        public static void AddMongoDMHangfire<TDBContext, TDBContexImpl, TProxyGenerator>(this IServiceCollection services)
            where TDBContext : class, IDBContextBase
            where TDBContexImpl : class, TDBContext
            where TProxyGenerator: class, IProxyGenerator
        {
            services.AddMongoDM<TDBContext, TDBContexImpl, TProxyGenerator, TaskRunner>();

            // Add Hangfire filters.
            GlobalJobFilters.Filters.Add(new LocalContextFilter(AsyncLocalContextAccessor.Instance));
        }
    }
}
