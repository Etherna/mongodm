using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Utility;

#nullable enable
namespace Digicando.MongODM.Serialization
{
    public interface IModelSerializerCollector
    {
        // Methods.
        void Register(IDbCache dbCache,
            IDbContext dbContext,
            IDocumentSchemaRegister documentSchemaRegister,
            IProxyGenerator proxyGenerator);
    }
}
