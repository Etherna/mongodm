using Digicando.MongODM.ProxyModels;
using Digicando.MongODM.Utility;

namespace Digicando.MongODM.Serialization
{
    public interface IModelSerializerCollector
    {
        // Methods.
        void Register(IDBCache dbCache,
            IDocumentSchemaRegister documentSchemaRegister,
            IProxyGenerator proxyGenerator);
    }
}
