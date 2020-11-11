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

using Etherna.MongODM.Core.Extensions;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    /// <summary>
    /// Identify a member map with a reference to root model map and its path
    /// </summary>
    public class MemberDependency
    {
        // Fields.
        private IEnumerable<BsonMemberMap> _memberPathToLastEntityModelId = default!;

        // Constructors.
        public MemberDependency(
            IModelMap rootModelMap,
            IEnumerable<BsonMemberMap> memberPath,
            bool? useCascadeDelete)
        {
            MemberPath = memberPath ?? throw new ArgumentNullException(nameof(memberPath));
            if (!memberPath.Any())
                throw new ArgumentException("Member path can't be empty", nameof(memberPath));
            RootModelMap = rootModelMap ?? throw new ArgumentNullException(nameof(rootModelMap));
            UseCascadeDelete = useCascadeDelete;
        }

        // Properties.
        /// <summary>
        /// Description of all encountered entity models in member path
        /// </summary>
        public IEnumerable<BsonClassMap> EntityModelMapPath => MemberPath.Select(memberMap => memberMap.ClassMap)
                                                                         .Where(classMap => classMap.IsEntity());

        /// <summary>
        /// True if member is an entity Id
        /// </summary>
        public bool IsIdMember => MemberPath.Last().IsIdMember();

        /// <summary>
        /// True if member is contained into a referenced entity model
        /// </summary>
        public bool IsEntityReferenceMember => EntityModelMapPath.Count() >= 2;

        /// <summary>
        /// The full path from root type to current member
        /// </summary>
        public IEnumerable<BsonMemberMap> MemberPath { get; }

        /// <summary>
        /// The member path from the root type, to the id member of the entity model that owns current member
        /// </summary>
        /// <example>
        /// [(E)ntityModel, (V)alueObject, (->) !id member, (i>) id member]
        /// 
        /// MemberPath:
        ///  E-> V-> Ei>
        /// [ 0 , 1 , 2 ]
        /// return: members([0, 1, 2])
        /// 
        /// MemberPath:
        ///  E-> V-> E-> V->
        /// [ 0 , 1 , 2 , 3 ]
        /// return: members([0, 1, {Ei>}])
        /// 
        /// MemberPath:
        ///  V-> V-> V-> V->
        /// [ 0 , 1 , 2 , 3 ]
        /// return: members([ ])
        /// 
        /// MemberPath:
        ///  E-> V-> V-> V->
        /// [ 0 , 1 , 2 , 3 ]
        /// return: members([{Ei>}])
        /// </example>
        public IEnumerable<BsonMemberMap> MemberPathToLastEntityModelId
        {
            get
            {
                if (_memberPathToLastEntityModelId is null)
                {
                    int take = MemberPath.Count() - 1;
                    for (; take >= 0; take--)
                    {
                        if (MemberPath.ElementAt(take).ClassMap.IsEntity())
                            break;
                    }

                    _memberPathToLastEntityModelId = take >= 0 ? //if exists an entity
                        MemberPath.Take(take).Append(
                            MemberPath.ElementAt(take).ClassMap.IdMemberMap) :
                        Array.Empty<BsonMemberMap>();
                }
                return _memberPathToLastEntityModelId;
            }
        }

        /// <summary>
        /// The root owning model map
        /// </summary>
        public IModelMap RootModelMap { get; }

        /// <summary>
        /// True if requested to apply cascade delete
        /// </summary>
        public bool? UseCascadeDelete { get; }

        // Methods.
        public string MemberPathToString() =>
            string.Join(".", MemberPath.Select(member => member.MemberInfo.Name));

        public string FullPathToString() => $"{RootModelMap.ModelType.Name}.{MemberPathToString()}";

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            
            strBuilder.AppendLine(FullPathToString());
            strBuilder.AppendLine($"    modelMapId: {RootModelMap.Id}");
            strBuilder.AppendLine($"    entity: {string.Join("->", EntityModelMapPath.Select(cm => cm.ClassType.Name))}");
            strBuilder.AppendLine($"    isEntityRefMem: {IsEntityReferenceMember}");
            strBuilder.AppendLine($"    isIdMem: {IsIdMember}");
            strBuilder.AppendLine($"    cascadeDelete: {UseCascadeDelete?.ToString() ?? "null"}");

            return strBuilder.ToString();
        }
    }
}
