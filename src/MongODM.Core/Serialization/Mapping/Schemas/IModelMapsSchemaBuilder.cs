using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    public interface IModelMapsSchemaBuilder<TModel>
        where TModel : class
    {
        // Methods.
        /// <summary>
        /// Add a fallback serializer invoked in case of undefined schema id
        /// </summary>
        /// <param name="fallbackSerializer">Fallback serializer</param>
        /// <returns>This same model schema</returns>
        IModelMapsSchemaBuilder<TModel> AddFallbackCustomSerializer(
            IBsonSerializer<TModel> fallbackSerializer);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="id">The map Id</param>
        /// <param name="modelMapInitializer">The model map inizializer</param>
        /// <param name="baseModelMapId">Id of the base model map for this model map</param>
        /// <param name="customSerializer">Custom serializer</param>
        /// <returns>This same model schema configuration</returns>
        IModelMapsSchemaBuilder<TModel> AddSecondaryMap(
            string id,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            string? baseModelMapId = null,
            IBsonSerializer<TModel>? customSerializer = null);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="modelMap">The model map</param>
        /// <returns>This same model schema configuration</returns>
        IModelMapsSchemaBuilder<TModel> AddSecondaryMap(
            ModelMap<TModel> modelMap);
    }
}
