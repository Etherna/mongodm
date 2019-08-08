using Digicando.MongoDM.ProxyModels;

namespace Digicando.MongoDM.Utility
{
    public interface IDBMaintainer
    {
        void OnUpdatedModel<TKey>(IAuditable auditableModel, TKey modelId);
    }
}