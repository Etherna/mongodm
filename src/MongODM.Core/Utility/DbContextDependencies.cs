using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Repositories;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Serialization.Modifiers;

namespace Digicando.MongODM.Utility
{
    public class DbContextDependencies : IDbContextDependencies
    {
        public DbContextDependencies(
            IDbCache dbCache,
            IDbMaintainer dbMaintainer,
            IDocumentSchemaRegister documentSchemaRegister,
            IProxyGenerator proxyGenerator,
            IRepositoryRegister repositoryRegister,
            ISerializerModifierAccessor serializerModifierAccessor)
        {
            DbCache = dbCache;
            DbMaintainer = dbMaintainer;
            DocumentSchemaRegister = documentSchemaRegister;
            ProxyGenerator = proxyGenerator;
            RepositoryRegister = repositoryRegister;
            SerializerModifierAccessor = serializerModifierAccessor;
        }

        public IDbCache DbCache { get; }
        public IDbMaintainer DbMaintainer { get; }
        public IDocumentSchemaRegister DocumentSchemaRegister { get; }
        public IProxyGenerator ProxyGenerator { get; }
        public IRepositoryRegister RepositoryRegister { get; }
        public ISerializerModifierAccessor SerializerModifierAccessor { get; }
    }
}
