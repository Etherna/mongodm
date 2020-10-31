using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    class CustomSerializerSchema<TModel> : SchemaBase, ICustomSerializerSchema<TModel>
        where TModel : class
    {
        // Constructor.
        public CustomSerializerSchema(
            IBsonSerializer<TModel> customSerializer,
            bool requireCollectionMigration = false)
            : base(typeof(TModel), requireCollectionMigration)
        {
            ActiveSerializer = customSerializer;
        }

        // Properties.
        public override IBsonSerializer ActiveSerializer { get; }
        public override Type? ProxyModelType => default;
        public override bool? UseProxyModel => default;
    }
}
