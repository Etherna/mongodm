using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Modifiers;

namespace Etherna.MongODM.Utility
{
    public interface IDbContextDependencies
    {
        IDbCache DbCache { get; }
        IDbMaintainer DbMaintainer { get; }
        IDocumentSchemaRegister DocumentSchemaRegister { get; }
        IProxyGenerator ProxyGenerator { get; }
        IRepositoryRegister RepositoryRegister { get; }
        ISerializerModifierAccessor SerializerModifierAccessor { get; }
    }
}