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
using Etherna.ExecContext.AspNetCore;
using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Bson.Serialization.Conventions;
using Etherna.MongODM.AspNetCore;
using Etherna.MongODM.Core;
using Etherna.MongODM.Core.Conventions;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Tasks;
using Etherna.MongODM.Core.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Etherna.MongODM
{
    public static class ServiceCollectionExtensions
    {
        public static IMongODMConfiguration AddMongODM<TTaskRunner>(
            this IServiceCollection services,
            Action<MongODMOptions>? configureOptions = null)
            where TTaskRunner : class, ITaskRunner, ITaskRunnerBuilder =>
            AddMongODM<ProxyGenerator, TTaskRunner>(services, configureOptions);

        public static IMongODMConfiguration AddMongODM<TProxyGenerator, TTaskRunner>(
            this IServiceCollection services,
            Action<MongODMOptions>? configureOptions = null)
            where TProxyGenerator : class, IProxyGenerator
            where TTaskRunner : class, ITaskRunner, ITaskRunnerBuilder
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

                    // Freeze configuration into mongodm options.
                    configuration.Freeze(options);

                    // Link options to services.
                    taskRunnerBuilder.SetMongODMOptions(options);
                });

            services.AddExecutionContext();

            services.TryAddSingleton<IProxyGenerator, TProxyGenerator>();
            services.TryAddSingleton<ITaskRunner, TTaskRunner>();
            services.TryAddSingleton<ITaskRunnerBuilder>(sp => (TTaskRunner)sp.GetRequiredService<ITaskRunner>());

            /* Register discriminator convention on typeof(object) because we need a method to handle
             * default returned instance from static calls to BsonSerializer.LookupDiscriminatorConvention(Type).
             * Several points internal to drivers invoke this method, and we can't avoid it. We need to set the default.
             */
            var sp = services.BuildServiceProvider();
            var execContext = sp.GetRequiredService<IExecutionContext>();
            BsonSerializer.RegisterDiscriminatorConvention(typeof(object),
                new HierarchicalProxyTolerantDiscriminatorConvention("_t", execContext));

            /* For same motive of handle static calls to BsonSerializer.LookupSerializer(Type),
             * we need a way to inject a current context accessor. This is a modification on official drivers,
             * waiting an official implementation of serialization contexts.
             */
            BsonSerializer.SetSerializationContextAccessor(new SerializationContextAccessor(execContext));

            // DbContext internal.
            //dependencies
            /*****
             * Transient dependencies have to be injected only into DbContext instance,
             * and passed to other with Initialize() method. This because otherwise inside
             * the same dbContext different components could have different instances of the same component.
             */
            services.TryAddTransient<IBsonSerializerRegistry, BsonSerializerRegistry>();
            services.TryAddTransient<IDbCache, DbCache>();
            services.TryAddTransient<IDbDependencies, DbDependencies>();
            services.TryAddTransient<IDbMaintainer, DbMaintainer>();
            services.TryAddTransient<IDbMigrationManager, DbMigrationManager>();
            services.TryAddTransient<IDiscriminatorRegistry, DiscriminatorRegistry>();
            services.TryAddTransient<IMapRegistry, MapRegistry>();
            services.TryAddTransient<IRepositoryRegistry, RepositoryRegistry>();
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
