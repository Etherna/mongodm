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
        private readonly Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector;
        private readonly Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector;

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            string arrayUndefinedItemSymbol = "",
            string documentUndefinedElementSymbol = "",
            bool referToFinalItem = false) :
            this(memberMap,
                _ => arrayUndefinedItemSymbol,
                _ => documentUndefinedElementSymbol,
                referToFinalItem)
        { }

        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector,
            Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector,
            bool referToFinalItem = false)
        {
            MemberMap = memberMap;
            ReferToFinalItem = referToFinalItem;
            this.undefinedArrayIndexSymbolSelector = undefinedArrayIndexSymbolSelector;
            this.undefinedDocumentElementSymbolSelector = undefinedDocumentElementSymbolSelector;
        }

        // Properties.
        public IMemberMap MemberMap { get; }
        public bool ReferToFinalItem { get; }

        // Methods.
        public override RenderedFieldDefinition Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(MemberMap.RenderElementPath(
                    ReferToFinalItem,
                    undefinedArrayIndexSymbolSelector,
                    undefinedDocumentElementSymbolSelector),
                MemberMap.Serializer);
    }

    public class MemberMapFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        // Fields.
        private readonly IBsonSerializer<TField>? customFieldSerializer;
        private readonly Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector;
        private readonly Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector;

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            string arrayUndefinedItemSymbol = "",
            string documentUndefinedElementSymbol = "",
            bool referToFinalItem = false,
            IBsonSerializer<TField>? customFieldSerializer = null) :
            this(memberMap,
                _ => arrayUndefinedItemSymbol,
                _ => documentUndefinedElementSymbol,
                referToFinalItem,
                customFieldSerializer)
        { }

        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector,
            Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector,
            bool referToFinalItem = false,
            IBsonSerializer<TField>? customFieldSerializer = null)
        {
            this.customFieldSerializer = customFieldSerializer;
            MemberMap = memberMap;
            this.undefinedArrayIndexSymbolSelector = undefinedArrayIndexSymbolSelector;
            this.undefinedDocumentElementSymbolSelector = undefinedDocumentElementSymbolSelector;
            ReferToFinalItem = referToFinalItem;
        }

        // Properties.
        public IMemberMap MemberMap { get; }
        public bool ReferToFinalItem { get; }

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
                MemberMap.RenderElementPath(
                    ReferToFinalItem,
                    undefinedArrayIndexSymbolSelector,
                    undefinedDocumentElementSymbolSelector),
                valueSerializer,
                valueSerializer,
                MemberMap.Serializer);
        }
    }
}
