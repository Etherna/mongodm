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

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    /// <summary>
    /// Identify a member map with a reference to its root model map, and path to reach it
    /// </summary>
    public class MemberMap
    {
        // Constructors.
        public MemberMap(
            MemberPath definitionPath,
            bool rootModelMapIsActive,
            bool useCascadeDelete)
        {
            this.DefinitionPath = definitionPath ?? throw new ArgumentNullException(nameof(definitionPath));
            RootModelMapIsActive = rootModelMapIsActive;
            UseCascadeDelete = useCascadeDelete;

            { //MemberPathToLastEntityModelId
                int take = DefinitionPath.ModelMapsPath.Count() - 1;
                for (; take >= 0; take--)
                {
                    if (DefinitionPath.ModelMapsPath.ElementAt(take).OwnerClass.IsEntity)
                        break;
                }

                MemberPathToLastEntityModelId = new MemberPath(
                    take >= 0 ? //if exists an entity
                        DefinitionPath.ModelMapsPath.Take(take).Append((
                            DefinitionPath.ModelMapsPath.ElementAt(take).OwnerClass,
                            DefinitionPath.ModelMapsPath.ElementAt(take).OwnerClass.BsonClassMap.IdMemberMap)) :
                        Array.Empty<(IModelMap OwnerClass, BsonMemberMap Member)>());
            }
        }

        // Properties.
        public MemberPath DefinitionPath { get; }

        /// <summary>
        /// True if member is an entity Id
        /// </summary>
        public bool IsIdMember => DefinitionPath.ModelMapsPath.Last().Member.IsIdMember();

        /// <summary>
        /// True if member is contained into a referenced entity model
        /// </summary>
        public bool IsEntityReferenceMember => DefinitionPath.EntityModelMaps.Count() >= 2;

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
        public MemberPath MemberPathToLastEntityModelId { get; }

        /// <summary>
        /// The root owning model map
        /// </summary>
        public IModelMap RootModelMap => DefinitionPath.ModelMapsPath.First().OwnerClass;

        /// <summary>
        /// True if root model map is the currently active in schema
        /// </summary>
        public bool RootModelMapIsActive { get; }

        /// <summary>
        /// True if requested to apply cascade delete
        /// </summary>
        public bool UseCascadeDelete { get; }
    }
}
