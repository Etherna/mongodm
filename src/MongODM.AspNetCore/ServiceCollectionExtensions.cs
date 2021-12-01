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

using Etherna.ExecContext.AspNetCore;
using Etherna.MongODM.Core;
using Etherna.MongODM.Core.Conventions;
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Tasks;
using Etherna.MongODM.Core.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using System;

namespace Etherna.MongODM.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IMongODMConfiguration AddMongODM<TTaskRunner, TModelBase>(
            this IServiceCollection services,
            Action<MongODMOptions>? configureOptions = null)
            where TTaskRunner : class, ITaskRunner, ITaskRunnerBuilder
            where TModelBase : class, IModel => //needed because of this https://jira.mongodb.org/browse/CSHARP-3154
            AddMongODM<ProxyGenerator, TTaskRunner, TModelBase>(services, configureOptions);

        public static IMongODMConfiguration AddMongODM<TProxyGenerator, TTaskRunner, TModelBase>(
            this IServiceCollection services,
            Action<MongODMOptions>? configureOptions = null)
            where TProxyGenerator : class, IProxyGenerator
            where TTaskRunner : class, ITaskRunner, ITaskRunnerBuilder
            where TModelBase : class, IModel //needed because of this https://jira.mongodb.org/browse/CSHARP-3154
        {
            // MongODM generic configuration.
            var configuration = new MongODMConfiguration(services);

            services.AddOptions<MongODMOptions>()
                .Configure(configureOptions ?? (_ => { }))
                .PostConfigure<IProxyGenerator, ITaskRunnerBuilder>(
                (options, proxyGenerator, taskRunnerBuilder) =>
                {
                    // Register global conventions.
                    ConventionRegistry.Register("Enum string", new ConventionPack
                    {
                        new EnumRepresentationConvention(BsonType.String)
                    }, c => true);

                    BsonSerializer.RegisterDiscriminatorConvention(typeof(TModelBase),
                        new HierarchicalProxyTolerantDiscriminatorConvention("_t", proxyGenerator));
                    BsonSerializer.RegisterDiscriminatorConvention(typeof(EntityModelBase),
                        new HierarchicalProxyTolerantDiscriminatorConvention("_t", proxyGenerator));

                    // Freeze configuration into mongodm options.
                    configuration.Freeze(options);

                    // Link options to services.
                    taskRunnerBuilder.SetMongODMOptions(options);
                });

            services.AddExecutionContext();

            services.TryAddSingleton<IProxyGenerator, TProxyGenerator>();
            services.TryAddSingleton<ITaskRunner, TTaskRunner>();
            services.TryAddSingleton<ITaskRunnerBuilder>(sp => (TTaskRunner)sp.GetRequiredService<ITaskRunner>());

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
            services.TryAddTransient<IDiscriminatorRegister, DiscriminatorRegister>();
            services.TryAddTransient<IRepositoryRegister, RepositoryRegister>();
            services.TryAddTransient<ISchemaRegister, SchemaRegister>();
            services.TryAddSingleton<ISerializerModifierAccessor, SerializerModifierAccessor>();

            //tasks
            services.TryAddTransient<IMigrateDbContextTask, MigrateDbContextTask>();
            services.TryAddTransient<IUpdateDocDependenciesTask, UpdateDocDependenciesTask>();

            //castle proxy generator
            services.TryAddSingleton<Castle.DynamicProxy.IProxyGenerator>(new Castle.DynamicProxy.ProxyGenerator());

            return configuration;
        }
    }
}
