using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    public interface IReferenceModelMapsSchemaBuilder<TModel>
    {
        // Methods.
        /// <summary>
        /// Add a fallback serializer invoked in case of undefined schema id
        /// </summary>
        /// <param name="fallbackSerializer">Fallback serializer</param>
        /// <returns>This same model schema</returns>
        IReferenceModelMapsSchemaBuilder<TModel> AddFallbackCustomSerializer(
            IBsonSerializer<TModel> fallbackSerializer);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="id">The map Id</param>
        /// <param name="baseModelMapId">Id of the base model map for this model map</param>
        /// <param name="modelMapInitializer">The model map inizializer</param>
        /// <returns>This same model schema configuration</returns>
        IReferenceModelMapsSchemaBuilder<TModel> AddSecondaryMap(
            string id,
            string? baseModelMapId = null,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="modelMap">The model map</param>
        /// <returns>This same model schema configuration</returns>
        IReferenceModelMapsSchemaBuilder<TModel> AddSecondaryMap(
            ModelMap<TModel> modelMap);
    }
}
