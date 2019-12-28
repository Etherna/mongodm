using Digicando.MongoDM.Models;
using System;
using System.Collections.Generic;

namespace Digicando.MongoDM.Utility
{
    class DBCache : IDBCache
    {
        // Consts.
        private const string CacheKey = "DBCache";

        // Fields.
        private readonly IContextAccessorFacade contextAccessor;

        // Constructors.
        public DBCache(IContextAccessorFacade contextAccessor)
        {
            this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        }

        // Properties.
        public IReadOnlyDictionary<object, IEntityModel> LoadedModels
        {
            get
            {
                lock (contextAccessor.SyncRoot)
                    return GetScopedCache();
            }
        }

        // Methods.
        public void ClearCache()
        {
            lock (contextAccessor.SyncRoot)
                GetScopedCache().Clear();
        }

        public void AddModel<TModel>(object id, TModel model)
            where TModel : class, IEntityModel
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            lock (contextAccessor.SyncRoot)
                GetScopedCache().Add(id, model);
        }

        public void RemoveModel(object id)
        {
            lock (contextAccessor.SyncRoot)
                GetScopedCache().Remove(id);
        }

        // Helpers.
        private Dictionary<object, IEntityModel> GetScopedCache()
        {
            if (!contextAccessor.Items.ContainsKey(CacheKey))
                contextAccessor.AddItem(CacheKey, new Dictionary<object, IEntityModel>());

            return contextAccessor.Items[CacheKey] as Dictionary<object, IEntityModel>;
        }
    }
}
