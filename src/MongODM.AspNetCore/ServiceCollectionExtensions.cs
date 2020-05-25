using Etherna.ExecContext;
using Etherna.ExecContext.AsyncLocal;
using Etherna.MongODM;
using Etherna.MongODM.AspNetCore;
using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Modifiers;
using Etherna.MongODM.Tasks;
using Etherna.MongODM.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static MongODMConfiguration UseMongODM<TTaskRunner>(
            this IServiceCollection services,
            IEnumerable<IExecutionContext>? executionContexts = null)
            where TTaskRunner : class, ITaskRunner =>
            UseMongODM<ProxyGenerator, TTaskRunner>(services, executionContexts);

        public static MongODMConfiguration UseMongODM<TProxyGenerator, TTaskRunner>(
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

            return new MongODMConfiguration(services);
        }

        public static MongODMConfiguration AddDbContext<TDbContext>(
            this MongODMConfiguration config,
            Action<DbContextOptions<TDbContext>>? dbContextConfig = null)
            where TDbContext : class, IDbContext
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            // Register dbContext.
            config.Services.AddSingleton<TDbContext>();

            // Register options.
            var contextOptions = new DbContextOptions<TDbContext>();
            dbContextConfig?.Invoke(contextOptions);
            config.Services.AddSingleton(contextOptions);

            return config;
        }

        public static MongODMConfiguration AddDbContext<TDbContext, TDbContextImpl>(
            this MongODMConfiguration config,
            Action<DbContextOptions<TDbContextImpl>>? dbContextConfig = null)
            where TDbContext : class, IDbContext
            where TDbContextImpl : class, TDbContext
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            // Register dbContext.
            config.Services.AddSingleton<TDbContext, TDbContextImpl>();
            config.Services.AddSingleton(sp => sp.GetService<TDbContext>() as TDbContextImpl);

            // Register options.
            var contextOptions = new DbContextOptions<TDbContextImpl>();
            dbContextConfig?.Invoke(contextOptions);
            config.Services.AddSingleton(contextOptions);

            return config;
        }
    }
}
