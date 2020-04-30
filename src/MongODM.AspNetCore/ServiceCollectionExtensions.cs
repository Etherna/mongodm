using Digicando.ExecContext;
using Digicando.ExecContext.AsyncLocal;
using Digicando.MongODM;
using Digicando.MongODM.AspNetCore;
using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Repositories;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Serialization.Modifiers;
using Digicando.MongODM.Tasks;
using Digicando.MongODM.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void UseMongODM<TTaskRunner>(
            this IServiceCollection services,
            IEnumerable<IExecutionContext>? executionContexts = null)
            where TTaskRunner : class, ITaskRunner =>
            UseMongODM<ProxyGenerator, TTaskRunner>(services, executionContexts);

        public static void UseMongODM<TProxyGenerator, TTaskRunner>(
            this IServiceCollection services,
            IEnumerable<IExecutionContext>? executionContexts = null)
            where TProxyGenerator: class, IProxyGenerator
            where TTaskRunner: class, ITaskRunner
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.TryAddSingleton(serviceProvider =>
            {
                if (executionContexts is null || !executionContexts.Any())
                    executionContexts = new IExecutionContext[] //default
                    {
                        new HttpContextExecutionContext(serviceProvider.GetService<IHttpContextAccessor>()),
                        AsyncLocalContext.Instance
                    };

                return executionContexts.Count() == 1 ?
                    executionContexts.First() :
                    new ExecutionContextSelector(executionContexts);
            });
            services.TryAddSingleton<IProxyGenerator, TProxyGenerator>();
            services.TryAddSingleton<ITaskRunner, TTaskRunner>();

            // DbContext internal.
            //dependencies
            /*****
             * Transient dependencies have to be injected only into DbContext instance,
             * and passed to other with Initialize() method. This because otherwise inside
             * the same dbContext different components could have different instances of the same component.
             */
            services.TryAddTransient<IDbCache, DbCache>();
            services.TryAddTransient<IDbContextDependencies, DbContextDependencies>();
            services.TryAddTransient<IDbMaintainer, DbMaintainer>();
            services.TryAddTransient<IDocumentSchemaRegister, DocumentSchemaRegister>();
            services.TryAddTransient<IRepositoryRegister, RepositoryRegister>();
            services.TryAddSingleton<ISerializerModifierAccessor, SerializerModifierAccessor>();

            //tasks
            services.TryAddTransient<IUpdateDocDependenciesTask, UpdateDocDependenciesTask>();

            //castle proxy generator.
            services.TryAddSingleton<Castle.DynamicProxy.IProxyGenerator>(new Castle.DynamicProxy.ProxyGenerator());
        }

        public static void UseMongODMDbContext<TDbContext>(
            this IServiceCollection services)
            where TDbContext : class, IDbContext
        {
            services.AddSingleton<TDbContext>();
        }

        public static void UseMongODMDbContext<TDbContext, TDbContextImpl>(
            this IServiceCollection services)
            where TDbContext : class, IDbContext
            where TDbContextImpl : class, TDbContext
        {
            services.AddSingleton<TDbContext, TDbContextImpl>();
            services.AddSingleton(sp => sp.GetService<TDbContext>() as TDbContextImpl);
        }
    }
}
