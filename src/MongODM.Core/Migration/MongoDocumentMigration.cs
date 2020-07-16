using Etherna.MongODM.Models;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Migration
{
    /// <summary>
    /// Migrate documents of a collection from an older version to a newer
    /// </summary>
    /// <typeparam name="TModel">The model type</typeparam>
    /// <typeparam name="TKey">The model's key type</typeparam>
    public class MongoDocumentMigration<TModel, TKey> : MongoMigrationBase
        where TModel : class, IEntityModel<TKey>
    {
        // Fields.
        private readonly SemanticVersion minimumDocumentVersion;
        private readonly IMongoCollection<TModel> sourceCollection;

        // Constructors.
        public MongoDocumentMigration(
            ICollectionRepository<TModel, TKey> sourceCollection,
            SemanticVersion minimumDocumentVersion,
            string id)
            : base(id)
        {
            if (sourceCollection is null)
                throw new ArgumentNullException(nameof(sourceCollection));

            this.sourceCollection = sourceCollection.Collection;
            this.minimumDocumentVersion = minimumDocumentVersion;
        }

        // Methods.
        public override async Task<MigrationResult> MigrateAsync(
            int callbackEveryDocuments = 0,
            Func<long, Task>? callbackAsync = null,
            CancellationToken cancellationToken = default)
        {
            if (callbackEveryDocuments < 0)
                throw new ArgumentOutOfRangeException(nameof(callbackEveryDocuments), "Value can't be negative");

            // Building filter.
            var filterBuilder = Builders<TModel>.Filter;
            var filter = filterBuilder.Or(
                // No version in document (very old).
                filterBuilder.Exists(DbContext.DocumentVersionElementName, false),

                // Version as string (doc.Version < "0.12.0").
                //(can't query directly for string because https://docs.mongodb.com/v3.2/reference/operator/query/type/#arrays)
                filterBuilder.Not(filterBuilder.Type(DbContext.DocumentVersionElementName, BsonType.Int32)),

                // Version is an array with values ("0.12.0" <= doc.Version).
                //doc.Major < min.Major
                filterBuilder.Lt($"{DbContext.DocumentVersionElementName}.0", minimumDocumentVersion.MajorRelease),

                //doc.Major == min.Major && doc.Minor < min.Minor
                filterBuilder.And(
                    filterBuilder.Eq($"{DbContext.DocumentVersionElementName}.0", minimumDocumentVersion.MajorRelease),
                    filterBuilder.Lt($"{DbContext.DocumentVersionElementName}.1", minimumDocumentVersion.MinorRelease)),

                //doc.Major == min.Major && doc.Minor == min.Minor && doc.Patch < min.Patch
                filterBuilder.And(
                    filterBuilder.Eq($"{DbContext.DocumentVersionElementName}.0", minimumDocumentVersion.MajorRelease),
                    filterBuilder.Eq($"{DbContext.DocumentVersionElementName}.1", minimumDocumentVersion.MinorRelease),
                    filterBuilder.Lt($"{DbContext.DocumentVersionElementName}.2", minimumDocumentVersion.PatchRelease)));

            // Migrate documents.
            var totMigratedDocuments = 0L;
            await sourceCollection.Find(filter, new FindOptions { NoCursorTimeout = true })
                .ForEachAsync(async model =>
                {
                    if (callbackEveryDocuments > 0 &&
                        totMigratedDocuments % callbackEveryDocuments == 0 &&
                        callbackAsync != null)
                        await callbackAsync.Invoke(totMigratedDocuments).ConfigureAwait(false);

                    await sourceCollection.ReplaceOneAsync(Builders<TModel>.Filter.Eq(m => m.Id, model.Id), model).ConfigureAwait(false);

                    totMigratedDocuments++;
                }, cancellationToken).ConfigureAwait(false);

            return MigrationResult.Succeeded(totMigratedDocuments);
        }
    }
}
