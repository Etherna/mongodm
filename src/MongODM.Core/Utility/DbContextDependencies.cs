using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Modifiers;

namespace Etherna.MongODM.Utility
{
    public class DbContextDependencies : IDbContextDependencies
    {
        public DbContextDependencies(
            IDbContextCache dbCache,
            IDbContextMaintainer dbMaintainer,
            IDbContextMigrationManager dbContextMigrationManager,
            IDocumentSchemaRegister documentSchemaRegister,
            IProxyGenerator proxyGenerator,
            IRepositoryRegister repositoryRegister,
            ISerializerModifierAccessor serializerModifierAccessor,
#pragma warning disable IDE0060 // Remove unused parameter. It's needed for run static configurations
#pragma warning disable CA1801 // Review unused parameters. Same of above
            IStaticConfigurationBuilder staticConfigurationBuilder)
#pragma warning restore CA1801 // Review unused parameters
#pragma warning restore IDE0060 // Remove unused parameter
        {
            DbContextCache = dbCache;
            DbContextMaintainer = dbMaintainer;
            DbContextMigrationManager = dbContextMigrationManager;
            DocumentSchemaRegister = documentSchemaRegister;
            ProxyGenerator = proxyGenerator;
            RepositoryRegister = repositoryRegister;
            SerializerModifierAccessor = serializerModifierAccessor;
        }

        public IDbContextCache DbContextCache { get; }
        public IDbContextMaintainer DbContextMaintainer { get; }
        public IDbContextMigrationManager DbContextMigrationManager { get; }
        public IDocumentSchemaRegister DocumentSchemaRegister { get; }
        public IProxyGenerator ProxyGenerator { get; }
        public IRepositoryRegister RepositoryRegister { get; }
        public ISerializerModifierAccessor SerializerModifierAccessor { get; }
    }
}
