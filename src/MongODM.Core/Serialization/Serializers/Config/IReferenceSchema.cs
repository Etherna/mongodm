using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization.Serializers.Config
{
    public interface IReferenceSchema : IFreezableConfig
    {
        // Properties.
        ReferenceModelMap ActiveMap { get; }
        IDictionary<string, ReferenceModelMap> AllMapsDictionary { get; }
        IBsonSerializer? FallbackSerializer { get; }
        Type ModelType { get; }
        Type? ProxyModelType { get; }
        IEnumerable<ReferenceModelMap> SecondaryMaps { get; }
    }

    public interface IReferenceSchema<TModel> : IReferenceSchema
    {
        // Methods.
        /// <summary>
        /// Add a fallback serializer invoked in case of undefined schema id
        /// </summary>
        /// <param name="fallbackSerializer">Fallback serializer</param>
        /// <returns>This same model schema</returns>
        IReferenceSchema<TModel> AddFallbackCustomSerializer(
            IBsonSerializer<TModel> fallbackSerializer);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="id">The map Id</param>
        /// <param name="baseModelMapId">Id of the base model map for this model map</param>
        /// <param name="modelMapInitializer">The model map inizializer</param>
        /// <returns>This same model schema configuration</returns>
        IReferenceSchema<TModel> AddSecondaryMap(
            string id,
            string? baseModelMapId = null,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null);

        /// <summary>
        /// Register a secondary model map
        /// </summary>
        /// <param name="modelMap">The model map</param>
        /// <returns>This same model schema configuration</returns>
        IReferenceSchema<TModel> AddSecondaryMap(
            ReferenceModelMap<TModel> modelMap);
    }
}
