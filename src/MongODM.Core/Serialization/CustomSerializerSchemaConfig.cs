using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization
{
    class CustomSerializerSchemaConfig<TModel> : SchemaConfigBase, ICustomSerializerSchemaConfig<TModel>
        where TModel : class
    {
        // Constructor.
        public CustomSerializerSchemaConfig(
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
