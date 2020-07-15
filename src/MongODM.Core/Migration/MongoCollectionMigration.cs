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
        private readonly Func<TModelSource, TModelDest> converter;
        private readonly Func<TModelSource, bool> discriminator;
        private readonly IMongoCollection<TModelDest> destinationCollection;
        private readonly IMongoCollection<TModelSource> sourceCollection;

        public MongoCollectionMigration(
            ICollectionRepository<TModelSource, TKeySource> sourceCollection,
            ICollectionRepository<TModelDest, TKeyDest> destinationCollection,
            Func<TModelSource, TModelDest> converter,
            Func<TModelSource, bool> discriminator)
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

        public override Task MigrateAsync(CancellationToken cancellationToken = default) =>
            sourceCollection.Find(Builders<TModelSource>.Filter.Empty, new FindOptions { NoCursorTimeout = true })
                .ForEachAsync(obj =>
                {
                    if (discriminator(obj))
                        destinationCollection.InsertOneAsync(converter(obj));
                }, cancellationToken);
    }
}
