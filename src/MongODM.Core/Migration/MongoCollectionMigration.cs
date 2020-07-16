using Etherna.MongODM.Models;
using Etherna.MongODM.Repositories;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Migration
{
    /// <summary>
    /// Migrate a collection to another
    /// </summary>
    /// <typeparam name="TModelSource">Type of source model</typeparam>
    /// <typeparam name="TKeySource">Type of source key</typeparam>
    /// <typeparam name="TModelDest">Type of destination model</typeparam>
    /// <typeparam name="TKeyDest">Type of destination key</typeparam>
    public class MongoCollectionMigration<TModelSource, TKeySource, TModelDest, TKeyDest> : MongoMigrationBase
        where TModelSource : class, IEntityModel<TKeySource>
        where TModelDest : class, IEntityModel<TKeyDest>
    {
        // Fields.
        private readonly Func<TModelSource, TModelDest> converter;
        private readonly Func<TModelSource, bool> discriminator;
        private readonly IMongoCollection<TModelDest> destinationCollection;
        private readonly IMongoCollection<TModelSource> sourceCollection;

        // Constructor.
        public MongoCollectionMigration(
            ICollectionRepository<TModelSource, TKeySource> sourceCollection,
            ICollectionRepository<TModelDest, TKeyDest> destinationCollection,
            Func<TModelSource, TModelDest> converter,
            Func<TModelSource, bool> discriminator,
            string id)
            : base(id)
        {
            if (sourceCollection is null)
                throw new ArgumentNullException(nameof(sourceCollection));
            if (destinationCollection is null)
                throw new ArgumentNullException(nameof(destinationCollection));

            this.sourceCollection = sourceCollection.Collection;
            this.destinationCollection = destinationCollection.Collection;
            this.converter = converter;
            this.discriminator = discriminator;
        }

        // Methods.
        public override async Task<MigrationResult> MigrateAsync(
            int callbackEveryDocuments = 0,
            Func<long, Task>? callbackAsync = null,
            CancellationToken cancellationToken = default)
        {
            if (callbackEveryDocuments < 0)
                throw new ArgumentOutOfRangeException(nameof(callbackEveryDocuments), "Value can't be negative");

            // Migrate documents.
            var totMigratedDocuments = 0L;
            await sourceCollection.Find(Builders<TModelSource>.Filter.Empty, new FindOptions { NoCursorTimeout = true })
                .ForEachAsync(async model =>
                {
                    if (callbackEveryDocuments > 0 &&
                        totMigratedDocuments % callbackEveryDocuments == 0 &&
                        callbackAsync != null)
                        await callbackAsync.Invoke(totMigratedDocuments).ConfigureAwait(false);

                    if (discriminator(model))
                        await destinationCollection.InsertOneAsync(converter(model)).ConfigureAwait(false);

                    totMigratedDocuments++;
                }, cancellationToken).ConfigureAwait(false);

            return MigrationResult.Succeeded(totMigratedDocuments);
        }
    }
}
