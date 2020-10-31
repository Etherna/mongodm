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

using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    /// <summary>
    /// Identify a member map with a reference to its <see cref="ModelMap"/> and its path
    /// </summary>
    public class MemberMap : FreezableConfig
    {
        // Constructors.
        public MemberMap(
            ModelMap modelMap,
            IEnumerable<BsonMemberMap> memberPath,
            bool? useCascadeDelete)
        {
            ModelMap = modelMap;
            MemberPath = memberPath;
            UseCascadeDelete = useCascadeDelete;
        }

        // Properties.
        /// <summary>
        /// Description of all encountered entity models in member path
        /// </summary>
        public IEnumerable<BsonClassMap> EntityModelMapPath => MemberPath.Select(m => m.EntityModelMap!)
                                                                         .Where(cm => cm != null)
                                                                         .Distinct();

        /// <summary>
        /// True if member is an entity Id
        /// </summary>
        public bool IsIdMember => MemberPath.Last().IsId;

        /// <summary>
        /// True if member is contained into a referenced entity model
        /// </summary>
        public bool IsEntityReferenceMember => EntityModelMapPath.Count() >= 2;

        /// <summary>
        /// The full path from root type to current member
        /// </summary>
        public IEnumerable<BsonMemberMap> MemberPath { get; }

        public ModelMap ModelMap { get; }

        /// <summary>
        /// The member path from the root type, to the member that references the entity model owning current member
        /// </summary>
        public IEnumerable<BsonMemberMap> PathToMemberEntityModel
        {
            get
            {
                var lastEntityNestedMembers = MemberPath.Aggregate<EntityMember, (int counter, BsonClassMap? lastEntityClassMap), int>(
                    (1, null),
                    (acc, member) => member.EntityModelMap == acc.lastEntityClassMap ?
                        (acc.counter++, member.EntityModelMap) :
                        (1, member.EntityModelMap),
                    acc => acc.counter);
                var entityMemberDeep = MemberPath.Count() - lastEntityNestedMembers;

                return MemberPath.Take(entityMemberDeep);
            }
        }

        /// <summary>
        /// The member path from the root type, to the id member of the entity model that owns current member
        /// </summary>
        public IEnumerable<BsonMemberMap> PathToMemberEntityModelId
        {
            get
            {
                if (IsIdMember)
                    return MemberPath;

                var lastEntityModelMap = MemberPath.Last().EntityModelMap;
                if (lastEntityModelMap is null)
                    throw new InvalidOperationException("This model is not related to a an entity model with an Id");

                return PathToMemberEntityModel.Append(new EntityMember(lastEntityModelMap.IdMemberMap, lastEntityModelMap));
            }
        }

        /// <summary>
        /// True if requested to apply cascade delete
        /// </summary>
        public bool? UseCascadeDelete { get; }

        // Methods.
        public string MemberPathToString() =>
            string.Join(".", MemberPath.Select(member => member.MemberInfo.Name));

        public string FullPathToString() => $"{ModelMap.ModelType.Name}.{MemberPathToString()}";

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            
            strBuilder.AppendLine(FullPathToString());
            strBuilder.AppendLine($"    modelMapId: {ModelMap.Id}");
            strBuilder.AppendLine($"    entity: {string.Join("->", EntityModelMapPath.Select(cm => cm.ClassType.Name))}");
            strBuilder.AppendLine($"    isEntityRefMem: {IsEntityReferenceMember}");
            strBuilder.AppendLine($"    isIdMem: {IsIdMember}");
            strBuilder.AppendLine($"    cascadeDelete: {UseCascadeDelete?.ToString() ?? "null"}");

            return strBuilder.ToString();
        }

        // Protected methods.
        protected override void FreezeAction()
        {
            base.FreezeAction();
        }
    }
}
