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
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Utility;

namespace Etherna.MongODM.Core
{
    public interface IDbDependencies
    {
        IBsonSerializerRegistry BsonSerializerRegistry { get; }
        IDbCache DbCache { get; }
        IDbMaintainer DbMaintainer { get; }
        IDbMigrationManager DbMigrationManager { get; }
        IDiscriminatorRegistry DiscriminatorRegistry { get; }
        IExecutionContext ExecutionContext { get; }
        IMapRegistry MapRegistry { get; }
        IProxyGenerator ProxyGenerator { get; }
        IRepositoryRegistry RepositoryRegistry { get; }
        ISerializerModifierAccessor SerializerModifierAccessor { get; }
    }
}