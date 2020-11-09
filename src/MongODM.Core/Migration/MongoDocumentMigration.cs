//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Repositories;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Migration
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
        private readonly ICollectionRepository<TModel, TKey> _sourceCollection;

        // Constructors.
        public MongoDocumentMigration(ICollectionRepository<TModel, TKey> sourceCollection)
        {
            if (sourceCollection is null)
                throw new ArgumentNullException(nameof(sourceCollection));

            _sourceCollection = sourceCollection;
        }

        // Properties.
        public override ICollectionRepository SourceCollection => _sourceCollection;

        // Methods.
        public override async Task<MigrationResult> MigrateAsync(
            int callbackEveryTotDocuments = 0,
            Func<long, Task>? callbackAsync = null,
            CancellationToken cancellationToken = default)
        {
            if (callbackEveryTotDocuments < 0)
                throw new ArgumentOutOfRangeException(nameof(callbackEveryTotDocuments), "Value can't be negative");

            // Migrate all documents.
            var totMigratedDocuments = 0L;
            await _sourceCollection.Collection.Find(FilterDefinition<TModel>.Empty, new FindOptions { NoCursorTimeout = true })
                .ForEachAsync(async model =>
                {
                    if (callbackEveryTotDocuments > 0 &&
                        totMigratedDocuments % callbackEveryTotDocuments == 0 &&
                        callbackAsync != null)
                        await callbackAsync.Invoke(totMigratedDocuments).ConfigureAwait(false);

                    await _sourceCollection.Collection.ReplaceOneAsync(Builders<TModel>.Filter.Eq(m => m.Id, model.Id), model).ConfigureAwait(false);

                    totMigratedDocuments++;
                }, cancellationToken).ConfigureAwait(false);

            return MigrationResult.Succeeded(totMigratedDocuments);
        }
    }
}
