using Etherna.MongoDB.Bson.Serialization;
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

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            string arrayItemSymbol = "",
            bool referToArrayItem = false,
            int skipElementsInPath = 0) :
            this(memberMap, mm => referToArrayItem || mm != memberMap ? arrayItemSymbol : "", skipElementsInPath)
        { }

        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            Func<IMemberMap, string> arrayItemSymbolSelector,
            int skipElementsInPath = 0)
        {
            this.arrayItemSymbolSelector = arrayItemSymbolSelector;
            MemberMap = memberMap;
            SkipElementsInPath = skipElementsInPath;
        }

        // Properties.
        private IMemberMap MemberMap { get; }
        public int SkipElementsInPath { get; }

        // Methods.
        public override RenderedFieldDefinition Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(MemberMap.GetElementPath(arrayItemSymbolSelector, SkipElementsInPath),
                MemberMap.Serializer);
    }

    public class MemberMapFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        // Fields.
        private readonly Func<IMemberMap, string> arrayItemSymbolSelector;
        private readonly IBsonSerializer<TField>? customFieldSerializer;

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            string arrayItemSymbol = "",
            IBsonSerializer<TField>? customFieldSerializer = null,
            int skipElementsInPath = 0,
            bool referToArrayItem = false) :
            this(memberMap, mm => referToArrayItem || mm != memberMap ? arrayItemSymbol : "", customFieldSerializer, skipElementsInPath)
        { }

        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            Func<IMemberMap, string> arrayItemSymbolSelector,
            IBsonSerializer<TField>? customFieldSerializer = null,
            int skipElementsInPath = 0)
        {
            this.arrayItemSymbolSelector = arrayItemSymbolSelector;
            this.customFieldSerializer = customFieldSerializer;
            MemberMap = memberMap;
            SkipElementsInPath = skipElementsInPath;
        }

        // Properties.
        private IMemberMap MemberMap { get; }
        public int SkipElementsInPath { get; }

        // Methods.
        public override RenderedFieldDefinition<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            Render(documentSerializer, serializerRegistry, linqProvider, false);

        public override RenderedFieldDefinition<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider, bool allowScalarValueForArrayField)
        {
            IBsonSerializer<TField> valueSerializer =
                customFieldSerializer ??
                (IBsonSerializer<TField>)FieldValueSerializerHelper.GetSerializerForValueType(
                    MemberMap.Serializer,
                    MemberMap.DbContext.SerializerRegistry,
                    typeof(TField),
                    allowScalarValueForArrayField);

            return new RenderedFieldDefinition<TField>(
                MemberMap.GetElementPath(arrayItemSymbolSelector, SkipElementsInPath),
                valueSerializer,
                valueSerializer,
                MemberMap.Serializer);
        }
    }
}
