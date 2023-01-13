using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using Etherna.MongODM.Core.Serialization.Mapping;
using System.Linq;

namespace Etherna.MongODM.Core.FieldDefinition
{
    public class MemberMapFieldDefinition<TDocument> : FieldDefinition<TDocument>
    {
        // Fields.
        private readonly IMemberMap memberMap;

        // Constructor.
        public MemberMapFieldDefinition(IMemberMap memberMap)
        {
            this.memberMap = memberMap;
        }

        // Methods.
        public override RenderedFieldDefinition Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(string.Join(".", memberMap.MemberMapPath.Select(mm => mm.BsonMemberMap.ElementName)),
                memberMap.ModelMapSchema.Serializer);
    }

    public class MemberMapFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        // Fields.
        private readonly IBsonSerializer<TField>? customFieldSerializer;
        private readonly IMemberMap memberMap;

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            IBsonSerializer<TField>? customFieldSerializer = null)
        {
            this.memberMap = memberMap;
            this.customFieldSerializer = customFieldSerializer;
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
                memberMap.ElementPath,
                valueSerializer,
                valueSerializer,
                memberMap.Serializer);
        }
    }
}
