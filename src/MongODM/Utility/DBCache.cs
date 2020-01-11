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
        private readonly IContext context;

        // Constructors.
        public DBCache(IContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Properties.
        public IReadOnlyDictionary<object, IEntityModel> LoadedModels
        {
            get
            {
                lock (context.Items)
                    return GetScopedCache();
            }
        }

        // Methods.
        public void ClearCache()
        {
            lock (context.Items)
                GetScopedCache().Clear();
        }

        public void AddModel<TModel>(object id, TModel model)
            where TModel : class, IEntityModel
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            lock (context.Items)
                GetScopedCache().Add(id, model);
        }

        public void RemoveModel(object id)
        {
            lock (context.Items)
                GetScopedCache().Remove(id);
        }

        // Helpers.
        private Dictionary<object, IEntityModel> GetScopedCache()
        {
            if (!context.Items.ContainsKey(CacheKey))
                context.Items.Add(CacheKey, new Dictionary<object, IEntityModel>());

            return context.Items[CacheKey] as Dictionary<object, IEntityModel>;
        }
    }
}
