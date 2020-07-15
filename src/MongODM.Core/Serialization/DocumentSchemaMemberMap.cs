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

using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Etherna.MongODM.Serialization
{
    public class DocumentSchemaMemberMap
    {
        // Constructors.
        public DocumentSchemaMemberMap(
            Type rootModelType,
            IEnumerable<EntityMember> memberPath,
            DocumentVersion version,
            bool? useCascadeDelete)
        {
            MemberPath = memberPath;
            RootModelType = rootModelType;
            UseCascadeDelete = useCascadeDelete;
            Version = version;
        }

        // Properties.
        public IEnumerable<BsonClassMap> EntityClassMapPath => MemberPath.Select(m => m.EntityClassMap!)
                                                                         .Where(cm => cm != null)
                                                                         .Distinct();
        public bool IsIdMember => MemberPath.Last().IsId;
        public bool IsEntityReferenceMember => EntityClassMapPath.Count() >= 2;
        public IEnumerable<EntityMember> MemberPath { get; }
        public IEnumerable<EntityMember> MemberPathToEntity
        {
            get
            {
                var lastEntityNestedMembers = MemberPath.Aggregate<EntityMember, (int counter, BsonClassMap? lastEntityClassMap), int>(
                    (1, null),
                    (acc, member) => member.EntityClassMap == acc.lastEntityClassMap ?
                        (acc.counter++, acc.lastEntityClassMap) :
                        (1, member.EntityClassMap),
                    acc => acc.counter);
                var idMemberDeep = MemberPath.Count() - lastEntityNestedMembers;

                return MemberPath.Take(idMemberDeep);
            }
        }
        public IEnumerable<EntityMember> MemberPathToId
        {
            get
            {
                if (IsIdMember)
                    return MemberPath;

                var lastEntityClassMap = MemberPath.Last().EntityClassMap;
                if (lastEntityClassMap is null)
                    throw new InvalidOperationException("This model is not related to a an entity model with an Id");

                return MemberPathToEntity.Append(new EntityMember(lastEntityClassMap, lastEntityClassMap.IdMemberMap));
            }
        }
        public Type RootModelType { get; }
        public bool? UseCascadeDelete { get; }
        public DocumentVersion Version { get; }

        // Methods.
        public string MemberPathToString() =>
            string.Join(".", MemberPath.Select(member => member.MemberMap.MemberInfo.Name));

        public string FullPathToString() => $"{RootModelType.Name}.{MemberPathToString()}";

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            
            strBuilder.AppendLine(FullPathToString());
            strBuilder.AppendLine($"    version: {Version}");
            strBuilder.AppendLine($"    entity: {string.Join("->", EntityClassMapPath.Select(cm => cm.ClassType.Name))}");
            strBuilder.AppendLine($"    isEntityRefMem: {IsEntityReferenceMember}");
            strBuilder.AppendLine($"    isIdMem: {IsIdMember}");
            strBuilder.AppendLine($"    cascadeDelete: {UseCascadeDelete?.ToString() ?? "null"}");

            return strBuilder.ToString();
        }
    }
}
