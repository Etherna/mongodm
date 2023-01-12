using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using Etherna.MongODM.Core.Serialization.Mapping;
using System.Linq;

namespace Etherna.MongODM.Core.Utility
{
    public class MemberMapFieldDefinition<TDocument> : FieldDefinition<TDocument>
    {
        // Constructor.
        public MemberMapFieldDefinition(IMemberMap memberMap)
        {
            MemberMap = memberMap;
        }

        // Properties.
        public IMemberMap MemberMap { get; }

        // Methods.
        public override RenderedFieldDefinition Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(string.Join(".", MemberMap.MemberMapPath.Select(mm => mm.BsonMemberMap.ElementName)),
                MemberMap.ModelMapSchema.Serializer);
    }

    public class MemberMapFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        // Fields.
        private readonly IBsonSerializer<TField>? customFieldSerializer;

        // Constructor.
        public MemberMapFieldDefinition(
            IMemberMap memberMap,
            IBsonSerializer<TField>? customFieldSerializer = null)
        {
            MemberMap = memberMap;
            this.customFieldSerializer = customFieldSerializer;
        }

        // Properties.
        public IMemberMap MemberMap { get; }

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
                MemberMap.ElementPath,
                valueSerializer,
                valueSerializer,
                MemberMap.Serializer);
        }
    }
}
