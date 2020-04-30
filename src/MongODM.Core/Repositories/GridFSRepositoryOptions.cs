using MongoDB.Bson;
using System;

namespace Digicando.MongODM.Repositories
{
    public class GridFSRepositoryOptions<TModel> : RepositoryOptionsBase
    {
        public GridFSRepositoryOptions(string name)
            : base(name)
        { }

        public Action<BsonDocument, TModel>? MetadataDeserializer { get; set; }
        public Func<TModel, BsonDocument>? MetadataSerializer { get; set; }
    }
}
