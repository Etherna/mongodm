using Digicando.MongoDM.ProxyModels;
using Digicando.MongoDM.Serialization;
using Digicando.MongoDM.Serialization.Modifiers;
using Digicando.MongoDM.Tasks;
using Digicando.MongoDM.Utility;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Digicando.MongoDM
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMongoDM<TDBContext, TDBContexImpl, TProxyGenerator>(this IServiceCollection services)
            where TDBContext : class, IDBContextBase
            where TDBContexImpl : class, TDBContext
            where TProxyGenerator: class, IProxyGenerator
        {
            services.AddSingleton<TDBContext, TDBContexImpl>();
            services.AddSingleton<IDBContextBase>(provider => provider.GetService<TDBContext>());

            services.AddSingleton<IContextAccessorFacade, ContextAccessorFacade>();
            services.AddSingleton<IDBCache, DBCache>();
            services.AddSingleton<IDBMaintainer, DBMaintainer>();
            services.AddSingleton<IDocumentSchemaRegister, DocumentSchemaRegister>();
            services.AddSingleton<IHangfireContextAccessor, HangfireContextAccessor>();
            services.AddSingleton<ISerializerModifierAccessor, SerializerModifierAccessor>();

            // Proxy generator.
            services.AddSingleton<IProxyGenerator, TProxyGenerator>();
            services.AddSingleton<Castle.DynamicProxy.IProxyGenerator>(new Castle.DynamicProxy.ProxyGenerator());

            // Add tasks.
            services.AddScoped<IUpdateDocDependenciesTask, UpdateDocDependenciesTask>();

            // Add Hangfire filters.
            GlobalJobFilters.Filters.Add(new HangfireContextAccessor());
        }
    }
}
