using Digicando.ExecContext;
using Digicando.MongODM.Models;
using System;
using System.Collections.Generic;

namespace Digicando.MongODM.Utility
{
    class DBCache : IDBCache
    {
        // Consts.
        private const string CacheKey = "DBCache";

        // Fields.
        private readonly IExecutionContext executionContext;

        // Constructors.
        public DBCache(IExecutionContext executionContext)
        {
            this.executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        }

        // Properties.
        public IReadOnlyDictionary<object, IEntityModel> LoadedModels
        {
            get
            {
                lock (executionContext.Items)
                    return GetScopedCache();
            }
        }

        // Methods.
        public void ClearCache()
        {
            lock (executionContext.Items)
                GetScopedCache().Clear();
        }

        public void AddModel<TModel>(object id, TModel model)
            where TModel : class, IEntityModel
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            lock (executionContext.Items)
                GetScopedCache().Add(id, model);
        }

        public void RemoveModel(object id)
        {
            lock (executionContext.Items)
                GetScopedCache().Remove(id);
        }

        // Helpers.
        private Dictionary<object, IEntityModel> GetScopedCache()
        {
            if (!executionContext.Items.ContainsKey(CacheKey))
                executionContext.Items.Add(CacheKey, new Dictionary<object, IEntityModel>());

            return executionContext.Items[CacheKey] as Dictionary<object, IEntityModel>;
        }
    }
}
