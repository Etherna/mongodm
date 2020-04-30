using Digicando.MongODM.ProxyModels;

#nullable enable
namespace Digicando.MongODM.Utility
{
    /// <summary>
    /// Interface for <see cref="DbMaintainer"/> implementation.
    /// </summary>
    public interface IDbMaintainer : IDbContextInitializable
    {
        // Methods.
        /// <summary>
        /// Method to invoke when an auditable model is changed.
        /// </summary>
        /// <typeparam name="TKey">The model type</typeparam>
        /// <param name="auditableModel">The changed model</param>
        /// <param name="modelId">The model id</param>
        void OnUpdatedModel<TKey>(IAuditable auditableModel, TKey modelId);
    }
}