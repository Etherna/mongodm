using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Repositories;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Serialization.Modifiers;

namespace Digicando.MongODM.Utility
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