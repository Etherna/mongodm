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
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IMemberMap
    {
        // Properties.
        IEnumerable<IMemberMap> AllDescendingMemberMaps { get; }

        BsonMemberMap BsonMemberMap { get; }

        IEnumerable<IMemberMap> ChildMemberMaps { get; }

        IDbContext DbContext { get; }

        bool ElementPathHasUndefinedArrayIndex { get; }

        bool ElementPathHasUndefinedDocumentElement { get; }

        /// <summary>
        /// An unique identifier
        /// </summary>
        string Id { get; }

        IEnumerable<ElementRepresentationBase> InternalElementPath { get; }

        /// <summary>
        /// True if member is contained into a referenced entity model
        /// </summary>
        bool IsEntityReferenceMember { get; }

        bool IsGeneratedByActiveSchemas { get; }

        /// <summary>
        /// True if member is an entity Id
        /// </summary>
        bool IsIdMember { get; }

        IEnumerable<IMemberMap> MemberMapPath { get; }

        IModelMapSchema ModelMapSchema { get; }

        IMemberMap? OwnerEntityIdMap { get; }

        IMemberMap? ParentMemberMap { get; }

        IBsonSerializer Serializer { get; }

        // Methods.
        string RenderElementPath(
            bool referToFinalItem,
            Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector,
            Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector);

        string RenderInternalItemElementPath(
            Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector,
            Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector);
    }
}