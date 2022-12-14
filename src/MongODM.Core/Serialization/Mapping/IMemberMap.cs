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

using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Serialization.Mapping.Schemas;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IMemberMap
    {
        // Properties.
        BsonMemberMap BsonMemberMap { get; }

        MemberPath DefinitionPath { get; }

        /// <summary>
        /// An unique identifier per schema
        /// </summary>
        string Id { get; }

        /// <summary>
        /// True if member is an entity Id
        /// </summary>
        bool IsIdMember { get; }

        /// <summary>
        /// True if member is contained into a referenced entity model
        /// </summary>
        bool IsEntityReferenceMember { get; }

        IModelMap? OwnerEntityModelMap { get; }

        IModelMap OwnerModelMap { get; }

        /// <summary>
        /// The root owning model map
        /// </summary>
        IModelMap RootModelMap { get; }

        IModelSchema Schema { get; }
    }
}