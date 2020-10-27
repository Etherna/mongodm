using MongoDB.Bson.Serialization;

namespace Etherna.MongODM.Core.Serialization
{
    class CustomSerializerSchemaConfiguration<TModel> : SchemaConfigurationBase, ICustomSerializerSchemaConfiguration<TModel>
        where TModel : class
    {
        // Constructor.
        public CustomSerializerSchemaConfiguration(
            IBsonSerializer<TModel> customSerializer,
            bool requireCollectionMigration = false)
            : base(typeof(TModel), requireCollectionMigration)
        {
            CustomSerializer = customSerializer;
        }

        // Properties.
        public IBsonSerializer CustomSerializer { get; }
    }
}
