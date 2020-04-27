using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Digicando.MongODM.Repositories
{
    public class CollectionRepositoryOptions<TModel> : RepositoryOptionsBase
    {
        public CollectionRepositoryOptions(string name)
            : base(name)
        {
            IndexBuilders = Array.Empty<(IndexKeysDefinition<TModel> keys, CreateIndexOptions<TModel> options)>();
        }

        public IEnumerable<(IndexKeysDefinition<TModel> keys, CreateIndexOptions<TModel> options)> IndexBuilders { get; set; }
    }
}
