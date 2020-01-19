﻿using Digicando.MongODM.ProxyModels;

namespace Digicando.MongODM.Utility
{
    /// <summary>
    /// Interface for <see cref="DBMaintainer"/> implementation.
    /// </summary>
    public interface IDBMaintainer
    {
        // Properties.
        bool IsInitialized { get; }

        // Methods.
        void Initialize(IDbContext dbContext);

        /// <summary>
        /// Method to invoke when an auditable model is changed.
        /// </summary>
        /// <typeparam name="TKey">The model type</typeparam>
        /// <param name="auditableModel">The changed model</param>
        /// <param name="modelId">The model id</param>
        void OnUpdatedModel<TKey>(IAuditable auditableModel, TKey modelId);
    }
}