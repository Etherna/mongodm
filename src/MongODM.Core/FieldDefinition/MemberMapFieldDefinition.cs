using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using Etherna.MongODM.Core.Serialization.Mapping;

namespace Etherna.MongODM.Core.FieldDefinition
{
    public class MemberMapFieldDefinition<TDocument> : FieldDefinition<TDocument>
    {
        // Fields.
        private readonly string arrayItemSymbol;
        private readonly IMemberMap memberMap;
        private readonly bool referToArrayItem;

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            string arrayItemSymbol = ".$",
            bool referToArrayItem = false)
        {
            this.arrayItemSymbol = arrayItemSymbol;
            this.memberMap = memberMap;
            this.referToArrayItem = referToArrayItem;
        }

        // Methods.
        public override RenderedFieldDefinition Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(memberMap.GetElementPath(arrayItemSymbol, referToArrayItem),
                memberMap.Serializer);
    }

    public class MemberMapFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        // Fields.
        private readonly string arrayItemSymbol;
        private readonly IBsonSerializer<TField>? customFieldSerializer;
        private readonly IMemberMap memberMap;
        private readonly bool referToArrayItem;

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            string arrayItemSymbol = "",
            IBsonSerializer<TField>? customFieldSerializer = null,
            bool referToArrayItem = false)
        {
            this.customFieldSerializer = customFieldSerializer;
            this.memberMap = memberMap;
            this.arrayItemSymbol = arrayItemSymbol;
            this.referToArrayItem = referToArrayItem;
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
                memberMap.GetElementPath(arrayItemSymbol, referToArrayItem),
                valueSerializer,
                valueSerializer,
                memberMap.Serializer);
        }
    }
}
