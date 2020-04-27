using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM.Migration
{
    /// <summary>
    /// Migrate a collection to another
    /// </summary>
    /// <typeparam name="TSource">Type of source model</typeparam>
    /// <typeparam name="TDest">Type of destination model</typeparam>
    public class MongoCollectionMigration<TSource, TDest> : MongoMigrationBase<TSource>
    {
        private readonly Func<TSource, TDest> converter;
        private readonly IMongoCollection<TDest> destinationCollection;
        private readonly Func<TSource, bool> discriminator;

        public MongoCollectionMigration(
            Func<TSource, bool> discriminator,
            Func<TSource, TDest> converter,
            IMongoCollection<TDest> destinationCollection,
            int priorityIndex)
            : base(priorityIndex)
        {
            this.converter = converter;
            this.destinationCollection = destinationCollection;
            this.discriminator = discriminator;
        }

        public override async Task MigrateAsync(IMongoCollection<TSource> sourceCollection, CancellationToken cancellationToken = default)
        {
            // Migrate documents.
            await sourceCollection.Find(Builders<TSource>.Filter.Empty, new FindOptions { NoCursorTimeout = true })
                .ForEachAsync(obj =>
                {
                    if (discriminator(obj))
                        destinationCollection.InsertOneAsync(converter(obj));
                }, cancellationToken);
        }
    }
}
