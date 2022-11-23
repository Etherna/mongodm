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
using Etherna.MongODM.Core.Extensions;
using System;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    /// <summary>
    /// Identify a member map with a reference to its root model map, and path to reach it
    /// </summary>
    public class MemberMap : IMemberMap
    {
        // Constructors.
        public MemberMap(
            MemberPath definitionPath)
        {
            DefinitionPath = definitionPath ?? throw new ArgumentNullException(nameof(definitionPath));
        }

        // Properties.
        public BsonMemberMap BsonMemberMap => DefinitionPath.ModelMapsPath.Last().Member;

        public MemberPath DefinitionPath { get; }

        public string Id => $"{RootModelMap.Id}|{DefinitionPath.TypedPathAsString}";

        /// <summary>
        /// True if member is contained into a referenced entity model
        /// </summary>
        public bool IsEntityReferenceMember => DefinitionPath.EntityModelMaps.Count() >= 2;

        /// <summary>
        /// True if member is an entity Id
        /// </summary>
        public bool IsIdMember => DefinitionPath.ModelMapsPath.Last().Member.IsIdMember();

        public IModelMap? OwnerEntityModelMap => DefinitionPath.EntityModelMaps.LastOrDefault();

        public IModelMap OwnerModelMap => DefinitionPath.ModelMapsPath.Last().OwnerModel;

        /// <summary>
        /// The root owning model map
        /// </summary>
        public IModelMap RootModelMap => DefinitionPath.ModelMapsPath.First().OwnerModel;
    }
}
