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
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    /// <summary>
    /// Identify a member map with a reference to its root model map, and path to reach it
    /// </summary>
    public class MemberMap : IMemberMap
    {
        // Fields.
        private readonly List<IMemberMap> _childMemberMaps = new();

        // Constructors.
        internal MemberMap(
            BsonMemberMap bsonMemberMap,
            IModelMapSchema modelMapSchema,
            IMemberMap? parentMemberMap)
        {
            BsonMemberMap = bsonMemberMap;
            ModelMapSchema = modelMapSchema;
            ParentMemberMap = parentMemberMap;
        }

        // Properties.
        public IEnumerable<IMemberMap> AllDescendingMemberMaps =>
            ChildMemberMaps.Concat(ChildMemberMaps.SelectMany(mm => mm.ChildMemberMaps));

        public BsonMemberMap BsonMemberMap { get; }

        public IEnumerable<IMemberMap> ChildMemberMaps => _childMemberMaps;

        public IEnumerable<IMemberMap> DefinitionMemberPath => ParentMemberMap is null ?
            new[] { this } :
            ParentMemberMap.DefinitionMemberPath.Concat(new[] { this });

        public string Id => ModelMapSchema.ModelMap.ModelType.Name + "|" + //<modelMapType>|<path;with;schema;ids>|<elementName>
            string.Join(";", DefinitionMemberPath.Select(mm => mm.ModelMapSchema.Id)) + "|" +
            BsonMemberMap.ElementName;

        /// <summary>
        /// True if member is contained into a referenced entity model
        /// </summary>
        public bool IsEntityReferenceMember => ModelMapSchema.IsEntity;

        /// <summary>
        /// True if member is an entity Id
        /// </summary>
        public bool IsIdMember => BsonMemberMap.IsIdMember();

        public IModelMapSchema ModelMapSchema { get; }

        public IMemberMap? ParentMemberMap { get; }

        // Internal methods.
        internal void AddChildMemberMap(IMemberMap childMemberMap) => _childMemberMaps.Add(childMemberMap);
    }
}
