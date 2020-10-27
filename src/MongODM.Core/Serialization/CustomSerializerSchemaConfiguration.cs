using MongoDB.Bson.Serialization;
using System;

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
            ActiveSerializer = customSerializer;
        }

        // Properties.
        public override IBsonSerializer ActiveSerializer { get; }
        public override Type? ProxyModelType => default;
        public override bool UseProxyModel => false;
    }
}
