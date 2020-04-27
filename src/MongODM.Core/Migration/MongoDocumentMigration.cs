using Digicando.MongODM.Models;
using Digicando.MongODM.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM.Migration
{
    /// <summary>
    /// Migrate documents of a collection from an older version to a newer
    /// </summary>
    /// <typeparam name="TModel">The model type</typeparam>
    /// <typeparam name="TKey">The model's key type</typeparam>
    public class MongoDocumentMigration<TModel, TKey> : MongoMigrationBase<TModel>
        where TModel : class, IEntityModel<TKey>
    {
        public MongoDocumentMigration(
            DocumentVersion minimumDocumentVersion,
            int priorityIndex = 0)
            : base(priorityIndex)
        {
            MinimumDocumentVersion = minimumDocumentVersion;
        }

        public DocumentVersion MinimumDocumentVersion { get; }

        /// <summary>
        /// Fix all documents prev of MinimumDocumentVersion
        /// </summary>
        public override async Task MigrateAsync(IMongoCollection<TModel> sourceCollection, CancellationToken cancellationToken = default)
        {
            var filterBuilder = Builders<TModel>.Filter;
            var filter = filterBuilder.Or(
                // No version in document (very old).
                filterBuilder.Exists(DbContext.DocumentVersionElementName, false),

                // Version as string (doc.Version < "0.12.0").
                //(can't query directly for string because https://docs.mongodb.com/v3.2/reference/operator/query/type/#arrays)
                filterBuilder.Not(filterBuilder.Type(DbContext.DocumentVersionElementName, BsonType.Int32)),

                // Version is an array with values ("0.12.0" <= doc.Version).
                //doc.Major < min.Major
                filterBuilder.Lt($"{DbContext.DocumentVersionElementName}.0", MinimumDocumentVersion.MajorRelease),

                //doc.Major == min.Major && doc.Minor < min.Minor
                filterBuilder.And(
                    filterBuilder.Eq($"{DbContext.DocumentVersionElementName}.0", MinimumDocumentVersion.MajorRelease),
                    filterBuilder.Lt($"{DbContext.DocumentVersionElementName}.1", MinimumDocumentVersion.MinorRelease)),

                //doc.Major == min.Major && doc.Minor == min.Minor && doc.Patch < min.Patch
                filterBuilder.And(
                    filterBuilder.Eq($"{DbContext.DocumentVersionElementName}.0", MinimumDocumentVersion.MajorRelease),
                    filterBuilder.Eq($"{DbContext.DocumentVersionElementName}.1", MinimumDocumentVersion.MinorRelease),
                    filterBuilder.Lt($"{DbContext.DocumentVersionElementName}.2", MinimumDocumentVersion.PatchRelease)));

            // Replace documents.
            await sourceCollection.Find(filter, new FindOptions { NoCursorTimeout = true })
                .ForEachAsync(obj => sourceCollection.ReplaceOneAsync(Builders<TModel>.Filter.Eq(m => m.Id, obj.Id), obj), cancellationToken);
        }
    }
}
