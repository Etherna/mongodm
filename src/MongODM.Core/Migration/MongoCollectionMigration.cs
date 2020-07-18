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
        private readonly ICollectionRepository<TModelSource, TKeySource> _sourceCollection;

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

            _sourceCollection = sourceCollection;
            this.destinationCollection = destinationCollection.Collection;
            this.converter = converter;
            this.discriminator = discriminator;
        }

        // Properties.
        public override ICollectionRepository SourceCollection => _sourceCollection;

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
            await _sourceCollection.Collection.Find(Builders<TModelSource>.Filter.Empty, new FindOptions { NoCursorTimeout = true })
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
