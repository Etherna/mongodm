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

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.Serialization.Conventions;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IDiscriminatorRegistry : IDbContextInitializable
    {
        void AddDiscriminator(Type type, BsonValue discriminator);

        void AddDiscriminatorConvention(Type type, IDiscriminatorConvention convention);

        /// <summary>
        /// Returns whether the given type has any discriminators registered for any of its subclasses.
        /// </summary>
        /// <param name="type">A Type.</param>
        /// <returns>True if the type is discriminated.</returns>
        bool IsTypeDiscriminated(Type type);

        /// <summary>
        /// Looks up the actual type of an object to be deserialized.
        /// </summary>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="discriminator">The discriminator.</param>
        /// <returns>The actual type of the object.</returns>
        Type LookupActualType(Type nominalType, BsonValue? discriminator);

        IDiscriminatorConvention LookupDiscriminatorConvention(Type type);
    }
}