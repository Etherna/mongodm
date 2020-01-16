using Digicando.ExecContext;
using Digicando.ExecContext.AsyncLocal;
using Digicando.MongODM.AspNetCore;
using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Serialization.Modifiers;
using Digicando.MongODM.Tasks;
using Digicando.MongODM.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Digicando.MongODM
{
    public static class ServiceCollectionExtensions
    {
        public static void UseMongODM<TProxyGenerator, TTaskRunner>(
            this IServiceCollection services,
            IEnumerable<IExecutionContext> executionContexts = null)
            where TProxyGenerator: class, IProxyGenerator
            where TTaskRunner: class, ITaskRunner
        {
            services.TryAddSingleton(serviceProvider =>
            {
                if (executionContexts is null || !executionContexts.Any())
                    executionContexts = new IExecutionContext[] //default
                    {
                        new HttpContextExecutionContext(serviceProvider.GetService<IHttpContextAccessor>()),
                        new AsyncLocalContext()
                    };

                return executionContexts.Count() == 1 ?
                    executionContexts.First() :
                    new ExecutionContextSelector(executionContexts);
            });
            services.TryAddSingleton<IProxyGenerator, TProxyGenerator>();
            services.TryAddSingleton<ITaskRunner, TTaskRunner>();

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
