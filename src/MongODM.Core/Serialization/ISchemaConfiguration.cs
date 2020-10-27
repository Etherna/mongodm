using System;

namespace Etherna.MongODM.Core.Serialization
{
    public interface ISchemaConfiguration
    {
        Type ModelType { get; }
        bool RequireCollectionMigration { get; }
    }
}