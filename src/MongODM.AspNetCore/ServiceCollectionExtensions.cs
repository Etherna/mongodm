//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Etherna.ExecContext;
using Etherna.ExecContext.AsyncLocal;
using Etherna.MongODM.AspNetCore;
using Etherna.MongODM.Core;
using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Tasks;
using Etherna.MongODM.Core.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IMongODMConfiguration AddMongODM<TTaskRunner, TModelBase>(
            this IServiceCollection services,
            Action<MongODMOptions>? configureOptions = null)
            where TTaskRunner : class, ITaskRunner
            where TModelBase : class, IModel => //needed because of this https://jira.mongodb.org/browse/CSHARP-3154
            AddMongODM<ProxyGenerator, TTaskRunner, TModelBase>(services, configureOptions);

        public static IMongODMConfiguration AddMongODM<TProxyGenerator, TTaskRunner, TModelBase>(
            this IServiceCollection services,
            Action<MongODMOptions>? configureOptions = null)
            where TProxyGenerator : class, IProxyGenerator
            where TTaskRunner : class, ITaskRunner
            where TModelBase : class, IModel //needed because of this https://jira.mongodb.org/browse/CSHARP-3154
        {
            // MongODM generic configuration.
            var configuration = new AspNetCoreMongODMConfiguration(services);
            services.TryAddSingleton<IMongODMConfiguration>(configuration);

            var mongODMOptions = new MongODMOptions();
            configureOptions?.Invoke(mongODMOptions);
            services.TryAddSingleton(mongODMOptions);

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.TryAddSingleton<IExecutionContext>(serviceProvider =>
               new ExecutionContextSelector(new IExecutionContext[] //default
               {
                    new HttpContextExecutionContext(serviceProvider.GetRequiredService<IHttpContextAccessor>()),
                    AsyncLocalContext.Instance
               }));
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
            services.TryAddTransient<IDbDependencies, DbDependencies>();
            services.TryAddTransient<IDbMaintainer, DbMaintainer>();
            services.TryAddTransient<IDbMigrationManager, DbMigrationManager>();
            services.TryAddTransient<IDocumentSchemaRegister, DocumentSchemaRegister>();
            services.TryAddTransient<IRepositoryRegister, RepositoryRegister>();
            services.TryAddSingleton<ISerializerModifierAccessor, SerializerModifierAccessor>();

            //tasks
            services.TryAddTransient<IMigrateDbContextTask, MigrateDbContextTask>();
            services.TryAddTransient<IUpdateDocDependenciesTask, UpdateDocDependenciesTask>();

            //castle proxy generator
            services.TryAddSingleton<Castle.DynamicProxy.IProxyGenerator>(new Castle.DynamicProxy.ProxyGenerator());

            //static configurations
            services.TryAddSingleton<IStaticConfigurationBuilder, StaticConfigurationBuilder<TModelBase>>();

            return configuration;
        }
    }
}
