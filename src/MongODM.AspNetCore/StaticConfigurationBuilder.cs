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

using Etherna.MongODM.Conventions;
using Etherna.MongODM.Operations;
using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Etherna.MongODM.AspNetCore
{
    public class StaticConfigurationBuilder<TModelBase> : IStaticConfigurationBuilder
    {
        public StaticConfigurationBuilder(IProxyGenerator proxyGenerator)
        {
            // Register conventions.
            ConventionRegistry.Register("Enum string", new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            }, c => true);

            BsonSerializer.RegisterDiscriminatorConvention(typeof(TModelBase),
                new HierarchicalProxyTolerantDiscriminatorConvention("_t", proxyGenerator));
            BsonSerializer.RegisterDiscriminatorConvention(typeof(OperationBase),
                new HierarchicalProxyTolerantDiscriminatorConvention("_t", proxyGenerator));
        }
    }
}
