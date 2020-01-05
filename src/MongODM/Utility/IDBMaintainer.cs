using Digicando.MongODM.ProxyModels;

namespace Digicando.MongODM.Utility
{
    public interface IDBMaintainer
    {
        void OnUpdatedModel<TKey>(IAuditable auditableModel, TKey modelId);
    }
}