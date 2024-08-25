﻿// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Bson.Serialization.Options;
using Etherna.MongODM.Core.Extensions;
using MoreLinq;
using System;
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
        private List<ElementRepresentationBase>? _internalElementPath;

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

        public bool ElementPathHasUndefinedArrayIndex => MemberMapPath.Any(mm => mm.InternalElementPath.OfType<ArrayElementRepresentation>().Any(e => e.ItemIndex == null));

        public bool ElementPathHasUndefinedDocumentElement => MemberMapPath.Any(mm => mm.InternalElementPath.OfType<DocumentElementRepresentation>().Any(e => e.ElementName == null));

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

        public IEnumerable<ElementRepresentationBase> InternalElementPath
        {
            get
            {
                if (_internalElementPath == null)
                {
                    _internalElementPath = new List<ElementRepresentationBase>();
                    var serializer = Serializer;

                    while (true)
                    {
                        /*
                         * Several serializers implements interfaces also if they are not able to provide required information.
                         * Because of this we have to try with different interfaces, if necessary.
                         * Start with more complex and go try simpler.
                         */

                        //dictionary
                        if (serializer is IBsonDictionarySerializer dictionarySerializer)
                        {
                            try
                            {
                                switch (dictionarySerializer.DictionaryRepresentation)
                                {
                                    case DictionaryRepresentation.ArrayOfArrays:
                                        _internalElementPath.Add(new ArrayElementRepresentation(this));
                                        _internalElementPath.Add(new ArrayElementRepresentation(this, 1));
                                        break;
                                    case DictionaryRepresentation.ArrayOfDocuments:
                                        _internalElementPath.Add(new ArrayElementRepresentation(this));
                                        _internalElementPath.Add(new DocumentElementRepresentation(this, "v"));
                                        break;
                                    case DictionaryRepresentation.Document:
                                        _internalElementPath.Add(new DocumentElementRepresentation(this));
                                        break;
                                    default: throw new NotSupportedException();
                                }
                                serializer = dictionarySerializer.ValueSerializer;
                                continue;
                            }
                            catch { }
                        }

                        //array
                        if (serializer is IBsonArraySerializer arraySerializer &&
                            arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
                        {
                            _internalElementPath.Add(new ArrayElementRepresentation(this));
                            serializer = itemSerializationInfo.Serializer;
                            continue;
                        }

                        // We tried all know types. We could be at final item serializer, or we could have found an unknown custom serializer.
                        break;
                    }
                }

                return _internalElementPath;
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
        public string RenderElementPath(
            bool referToFinalItem,
            Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector,
            Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector) =>
            MemberMapRenderHelper.RenderElementPath(MemberMapPath, referToFinalItem, undefinedArrayIndexSymbolSelector, undefinedDocumentElementSymbolSelector);

        public string RenderInternalItemElementPath(
            Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector,
            Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector) =>
            MemberMapRenderHelper.RenderInternalItemElementPath(InternalElementPath, undefinedArrayIndexSymbolSelector, undefinedDocumentElementSymbolSelector);

        // Internal methods.
        internal void AddChildMemberMap(IMemberMap childMemberMap) => _childMemberMaps.Add(childMemberMap);
    }
}
