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

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IMemberMap
    {
        // Properties.
        BsonMemberMap BsonMemberMap { get; }

        MemberPath DefinitionPath { get; }

        /// <summary>
        /// True if member is an entity Id
        /// </summary>
        bool IsIdMember { get; }

        /// <summary>
        /// True if member is contained into a referenced entity model
        /// </summary>
        bool IsEntityReferenceMember { get; }

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
        MemberPath MemberPathToLastEntityModelId { get; }

        /// <summary>
        /// The root owning model map
        /// </summary>
        IModelMap RootModelMap { get; }

        /// <summary>
        /// True if requested to apply cascade delete
        /// </summary>
        bool UseCascadeDelete { get; }
    }
}