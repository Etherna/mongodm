using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongoDB.Driver.Linq;
using System.Text;

namespace Etherna.MongODM.Core.FieldDefinition
{
    public class UnmappedFieldDefinition<TDocument> : FieldDefinition<TDocument>
    {
        // Fields.
        private readonly FieldDefinition<TDocument>? baseDocumentField;
        private readonly string fieldName;
        private readonly IBsonSerializer fieldSerializer;

        // Constructor.
        public UnmappedFieldDefinition(
            FieldDefinition<TDocument>? baseDocumentField,
            string fieldName,
            IBsonSerializer fieldSerializer)
        {
            this.baseDocumentField = baseDocumentField;
            this.fieldName = fieldName;
            this.fieldSerializer = fieldSerializer;
        }

        // Methods.
        public override RenderedFieldDefinition Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(UnmappedFieldDefinitionHelper.BuildFieldPath(baseDocumentField, fieldName, documentSerializer, serializerRegistry, linqProvider),
                fieldSerializer);
    }

    public class UnmappedFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        // Fields.
        private readonly FieldDefinition<TDocument>? baseDocumentField;
        private readonly string fieldName;
        private readonly IBsonSerializer<TField> fieldSerializer;

        // Constructor.
        public UnmappedFieldDefinition(
            FieldDefinition<TDocument>? baseDocumentField,
            string fieldName,
            IBsonSerializer<TField> fieldSerializer)
        {
            this.baseDocumentField = baseDocumentField;
            this.fieldName = fieldName;
            this.fieldSerializer = fieldSerializer;
        }

        // Methods.
        public override RenderedFieldDefinition<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider) =>
            new(UnmappedFieldDefinitionHelper.BuildFieldPath(baseDocumentField, fieldName, documentSerializer, serializerRegistry, linqProvider),
                fieldSerializer,
                fieldSerializer,
                fieldSerializer);
    }

    internal static class UnmappedFieldDefinitionHelper
    {
        public static string BuildFieldPath<TDocument>(
            FieldDefinition<TDocument>? baseDocumentField,
            string fieldName,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            LinqProvider linqProvider)
        {
            var sb = new StringBuilder();
            if (baseDocumentField is not null)
            {
                var baseDocRenderedField = baseDocumentField.Render(documentSerializer, serializerRegistry, linqProvider);
                sb.Append(baseDocRenderedField.FieldName);
            }
            if (sb.Length > 0)
                sb.Append('.');
            sb.Append(fieldName);

            return sb.ToString();
        }
    }
}
