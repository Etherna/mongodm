using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Modifiers;

namespace Etherna.MongODM.Utility
{
    public interface IDbContextDependencies
    {
        IDbContextCache DbContextCache { get; }
        IDbContextMaintainer DbContextMaintainer { get; }
        IDbContextMigrationManager DbContextMigrationManager { get; }
        IDocumentSchemaRegister DocumentSchemaRegister { get; }
        IProxyGenerator ProxyGenerator { get; }
        IRepositoryRegister RepositoryRegister { get; }
        ISerializerModifierAccessor SerializerModifierAccessor { get; }
    }
}