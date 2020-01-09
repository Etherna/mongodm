using Castle.DynamicProxy;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Serialization.Modifiers;
using Digicando.MongODM.Tasks;
using Digicando.MongODM.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Digicando.MongODM
{
    public static class ServiceCollectionExtensions
    {
        public static void UseMongoDbContext<TDbContext, TDbContextImpl>(
            this IServiceCollection services,
            DbContextOptions<TDbContextImpl> options)
            where TDbContext : class, IDbContext
            where TDbContextImpl : DbContext, TDbContext
        {
            services.AddSingleton<TDbContext, TDbContextImpl>();
            services.AddSingleton(options);

            // DbContext dependencies.
            services.TryAddTransient<IDBCache, DBCache>();
            services.TryAddTransient<IDBMaintainer, DBMaintainer>();
            services.TryAddTransient<IDocumentSchemaRegister, DocumentSchemaRegister>();
            services.TryAddTransient<ISerializerModifierAccessor, SerializerModifierAccessor>();

            // Castle proxy generator.
            services.TryAddSingleton<IProxyGenerator>(new ProxyGenerator());

            // Tasks.
            services.TryAddScoped<IUpdateDocDependenciesTask, UpdateDocDependenciesTask>();
        }
    }
}
