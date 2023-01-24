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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    /// <summary>
    /// Identify a member map with a reference to its root model map, and path to reach it
    /// </summary>
    public class MemberMap : IMemberMap
    {
        // Fields.
        private readonly List<IMemberMap> _childMemberMaps = new();
        private int? _maxArrayItemDepth;

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

        public IDbContext DbContext => ModelMapSchema.ModelMap.DbContext;

        //DefinitionMemberPath as: <modelMapType>;<schemaId>;<elementName>(|<modelMapType>;<schemaId>;<elementName>)*
        public string Id => string.Join("|", MemberMapPath.Select(
                mm => $"{mm.ModelMapSchema.ModelMap.ModelType.Name};{mm.ModelMapSchema.Id};{mm.BsonMemberMap.ElementName}"));

        /// <summary>
        /// True if member is contained into a referenced entity model
        /// </summary>
        public bool IsEntityReferenceMember => MemberMapPath.Where(mm => mm.ModelMapSchema.IsEntity)
                                                                   .Count() >= 2;

        public bool IsGeneratedByActiveSchemas => !MemberMapPath.Any(mm => !mm.ModelMapSchema.IsCurrentActive);

        /// <summary>
        /// True if member is an entity Id
        /// </summary>
        public bool IsIdMember => BsonMemberMap.IsIdMember();

        public bool IsSerializedAsArray => Serializer is IBsonArraySerializer;

        public int MaxArrayItemDepth
        {
            get
            {
                if (_maxArrayItemDepth == null)
                {
                    var serializer = Serializer;
                    var depth = 0;
                    while (serializer is IBsonArraySerializer arraySerializer)
                    {
                        depth++;
                        arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo);
                        serializer = itemSerializationInfo.Serializer;
                    }

                    _maxArrayItemDepth = depth;
                }

                return _maxArrayItemDepth.Value;
            }
        }

        public IEnumerable<IMemberMap> MemberMapPath => ParentMemberMap is null ?
            new[] { this } :
            ParentMemberMap.MemberMapPath.Concat(new[] { this });

        public IModelMapSchema ModelMapSchema { get; }

        public IMemberMap? OwnerEntityIdMap
        {
            get
            {
                // Search backward first entity.
                var entityMemberMap = MemberMapPath.Reverse()
                                                   .FirstOrDefault(mm => mm.ModelMapSchema.IsEntity)
                                                   ?.ParentMemberMap;

                // Search id member map with same schema of this.
                return entityMemberMap?.ChildMemberMaps
                    ?.Where(mm => mm.ModelMapSchema == ModelMapSchema)
                    ?.Single(mm => mm.IsIdMember);
            }
        }

        public IMemberMap? ParentMemberMap { get; }

        public IBsonSerializer Serializer => BsonMemberMap.GetSerializer();

        // Public methods.
        public string GetElementPath(Func<IMemberMap, string> arrayItemSymbolSelector, int skipElements = 0) =>
            PathBuilderHelper(arrayItemSymbolSelector, mm => mm.BsonMemberMap.ElementName, skipElements);

        public string GetMemberPath(Func<IMemberMap, string> arrayItemSymbolSelector, int skipElements = 0) =>
            PathBuilderHelper(arrayItemSymbolSelector, mm => mm.BsonMemberMap.MemberName, skipElements);

        // Internal methods.
        internal void AddChildMemberMap(IMemberMap childMemberMap) => _childMemberMaps.Add(childMemberMap);

        // Helpers.
        private string PathBuilderHelper(
            Func<IMemberMap, string> arrayItemSymbolSelector,
            Func<IMemberMap, string> extractNameFunc,
            int skipElements)
        {
            var stringBulder = new StringBuilder();

            foreach (var (memberMap, i) in MemberMapPath.Skip(skipElements).Select((mm, i) => (mm, i)))
            {
                if (i != 0) stringBulder.Append('.');

                stringBulder.Append(extractNameFunc(memberMap));

                if (memberMap.IsSerializedAsArray)
                    stringBulder.Append(arrayItemSymbolSelector(memberMap));
            }

            return stringBulder.ToString();
        }
    }
}
