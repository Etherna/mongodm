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
        public static void UseMongODM(this IServiceCollection services)
        {
            // DbContext internal.
            //dependencies
            services.TryAddTransient<IDBCache, DBCache>();
            services.TryAddTransient<IDBMaintainer, DBMaintainer>();
            services.TryAddTransient<IDocumentSchemaRegister, DocumentSchemaRegister>();
            services.TryAddTransient<ISerializerModifierAccessor, SerializerModifierAccessor>();

            //tasks
            services.TryAddTransient<IUpdateDocDependenciesTask, UpdateDocDependenciesTask>();

            //castle proxy generator.
            services.TryAddSingleton<Castle.DynamicProxy.IProxyGenerator>(new Castle.DynamicProxy.ProxyGenerator());
        }
    }
}
