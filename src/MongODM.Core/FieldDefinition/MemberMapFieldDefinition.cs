﻿using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using Etherna.MongODM.Core.Serialization.Mapping;
using System;

namespace Etherna.MongODM.Core.FieldDefinition
{
    public class MemberMapFieldDefinition<TDocument> : FieldDefinition<TDocument>
    {
        // Fields.
        private readonly Func<IMemberMap, string> arrayItemSymbolSelector;
        private readonly IMemberMap memberMap;

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            string arrayItemSymbol = "",
            bool referToArrayItem = false) :
            this(memberMap, mm => referToArrayItem || mm != memberMap ? arrayItemSymbol : "")
        { }

        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            Func<IMemberMap, string> arrayItemSymbolSelector)
        {
            this.arrayItemSymbolSelector = arrayItemSymbolSelector;
            this.memberMap = memberMap;
        }

        // Methods.
        public override RenderedFieldDefinition Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(memberMap.GetElementPath(arrayItemSymbolSelector),
                memberMap.Serializer);
    }

    public class MemberMapFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        // Fields.
        private readonly Func<IMemberMap, string> arrayItemSymbolSelector;
        private readonly IBsonSerializer<TField>? customFieldSerializer;
        private readonly IMemberMap memberMap;

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            string arrayItemSymbol = "",
            IBsonSerializer<TField>? customFieldSerializer = null,
            bool referToArrayItem = false) :
            this(memberMap, mm => referToArrayItem || mm != memberMap ? arrayItemSymbol : "", customFieldSerializer)
        { }

        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            Func<IMemberMap, string> arrayItemSymbolSelector,
            IBsonSerializer<TField>? customFieldSerializer = null)
        {
            this.arrayItemSymbolSelector = arrayItemSymbolSelector;
            this.customFieldSerializer = customFieldSerializer;
            this.memberMap = memberMap;
        }

        // Methods.
        public override RenderedFieldDefinition<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            Render(documentSerializer, serializerRegistry, linqProvider, false);

        public override RenderedFieldDefinition<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider, bool allowScalarValueForArrayField)
        {
            IBsonSerializer<TField> valueSerializer =
                customFieldSerializer ??
                (IBsonSerializer<TField>)FieldValueSerializerHelper.GetSerializerForValueType(
                    memberMap.Serializer,
                    memberMap.DbContext.SerializerRegistry,
                    typeof(TField),
                    allowScalarValueForArrayField);

            return new RenderedFieldDefinition<TField>(
                memberMap.GetElementPath(arrayItemSymbolSelector),
                valueSerializer,
                valueSerializer,
                memberMap.Serializer);
        }
    }
}