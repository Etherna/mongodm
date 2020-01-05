using Digicando.MongODM.Models;
using System.Collections.Generic;

namespace Digicando.MongODM.Utility
{
    public interface IDBCache
    {
        // Properties.
        IReadOnlyDictionary<object, IEntityModel> LoadedModels { get; }

        // Methods.
        void AddModel<TModel>(object id, TModel model)
            where TModel : class, IEntityModel;

        void ClearCache();

        void RemoveModel(object id);
    }
}