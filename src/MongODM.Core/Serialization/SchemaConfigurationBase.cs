using System;

namespace Etherna.MongODM.Core.Serialization
{
    abstract class SchemaConfigurationBase : ISchemaConfiguration
    {
        // Constructor.
        public SchemaConfigurationBase(
            Type modelType,
            bool requireCollectionMigration)
        {
            ModelType = modelType;
            RequireCollectionMigration = requireCollectionMigration;
        }

        // Properties.
        public Type ModelType { get; }
        public bool RequireCollectionMigration { get; }
    }
}
