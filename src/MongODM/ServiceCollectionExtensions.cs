using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Serialization.Modifiers;
using Digicando.MongODM.Tasks;
using Digicando.MongODM.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Digicando.MongODM
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMongoDM<TDbContext, TDbContextImpl, TProxyGenerator, TTaskRunner>(this IServiceCollection services)
            where TDbContext : class, IDbContext
            where TDbContextImpl : class, TDbContext
            where TProxyGenerator: class, IProxyGenerator
            where TTaskRunner: class, ITaskRunner
        {
            services.AddSingleton<TDbContext, TDbContextImpl>();
            services.AddSingleton<IDbContext>(provider => provider.GetService<TDbContext>());

            services.AddSingleton(AsyncLocalContextAccessor.Instance);
            services.AddSingleton<IContextAccessorFacade, ContextAccessorFacade>();
            services.AddSingleton<IDBCache, DBCache>();
            services.AddSingleton<IDBMaintainer, DBMaintainer>();
            services.AddSingleton<IDocumentSchemaRegister, DocumentSchemaRegister>();
            services.AddSingleton<ISerializerModifierAccessor, SerializerModifierAccessor>();

            // Proxy generator.
            services.AddSingleton<IProxyGenerator, TProxyGenerator>();
            services.AddSingleton<Castle.DynamicProxy.IProxyGenerator>(new Castle.DynamicProxy.ProxyGenerator());

            // Add tasks.
            services.AddSingleton<ITaskRunner, TTaskRunner>();
            services.AddScoped<IUpdateDocDependenciesTask, UpdateDocDependenciesTask>();
        }
    }
}
