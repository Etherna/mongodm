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

using Etherna.MongODM.ProxyModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using System;

namespace Etherna.MongODM.Conventions
{
    public class HierarchicalProxyTolerantDiscriminatorConvention : HierarchicalDiscriminatorConvention
    {
        // Fields.
        private readonly IProxyGenerator proxyGenerator;

        // Constructors.
        public HierarchicalProxyTolerantDiscriminatorConvention(
            string elementName,
            IProxyGenerator proxyGenerator)
            : base(elementName)
        {
            this.proxyGenerator = proxyGenerator ?? throw new ArgumentNullException(nameof(proxyGenerator));
        }

        // Methods.
        public override BsonValue GetDiscriminator(Type nominalType, Type actualType) =>
            base.GetDiscriminator(nominalType, proxyGenerator.PurgeProxyType(actualType));
    }
}
