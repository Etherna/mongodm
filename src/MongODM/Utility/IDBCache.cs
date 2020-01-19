using Digicando.MongODM.Models;
using System.Collections.Generic;

namespace Digicando.MongODM.Utility
{
    /// <summary>
    /// Interface for <see cref="DBCache"/> implementation.
    /// </summary>
    public interface IDBCache
    {
        // Properties.
        /// <summary>
        /// List of current cached models, indexed by Id.
        /// </summary>
        IReadOnlyDictionary<object, IEntityModel> LoadedModels { get; } // (id -> model)

        // Methods.
        /// <summary>
        /// Add a new model in cache.
        /// </summary>
        /// <typeparam name="TModel">The model type</typeparam>
        /// <param name="id">The model Id</param>
        /// <param name="model">The model</param>
        void AddModel<TModel>(object id, TModel model)
            where TModel : class, IEntityModel;

        /// <summary>
        /// Clear current cache archive.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Remove a model from cache.
        /// </summary>
        /// <param name="id">The model Id</param>
        void RemoveModel(object id);
    }
}