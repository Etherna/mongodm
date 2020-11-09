using MongoDB.Bson.Serialization;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class ReferenceModelMap<TModel> : ModelMapBase
    {
        // Constructors.
        public ReferenceModelMap(
            string id,
            BsonClassMap<TModel> bsonClassMap,
            string? baseModelMapId = null)
            : base(id, baseModelMapId, bsonClassMap, null)
        { }
    }
}
