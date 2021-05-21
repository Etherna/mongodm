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

using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;

namespace Etherna.MongODM.Core.Utility
{
    public class DbDependencies : IDbDependencies
    {
        public DbDependencies(
            IDbCache dbCache,
            IDbMaintainer dbMaintainer,
            IDbMigrationManager dbContextMigrationManager,
            IProxyGenerator proxyGenerator,
            IRepositoryRegister repositoryRegister,
            ISchemaRegister schemaRegister,
            ISerializerModifierAccessor serializerModifierAccessor,
#pragma warning disable IDE0060 // Remove unused parameter. It's needed for run static configurations
#pragma warning disable CA1801 // Review unused parameters
            IStaticConfigurationBuilder staticConfigurationBuilder)
#pragma warning restore CA1801 // Review unused parameters
#pragma warning restore IDE0060 // Remove unused parameter
        {
            DbCache = dbCache;
            DbMaintainer = dbMaintainer;
            DbMigrationManager = dbContextMigrationManager;
            SchemaRegister = schemaRegister;
            ProxyGenerator = proxyGenerator;
            RepositoryRegister = repositoryRegister;
            SerializerModifierAccessor = serializerModifierAccessor;
        }

        public IDbCache DbCache { get; }
        public IDbMaintainer DbMaintainer { get; }
        public IDbMigrationManager DbMigrationManager { get; }
        public IProxyGenerator ProxyGenerator { get; }
        public IRepositoryRegister RepositoryRegister { get; }
        public ISchemaRegister SchemaRegister { get; }
        public ISerializerModifierAccessor SerializerModifierAccessor { get; }
    }
}
