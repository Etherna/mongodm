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

using Etherna.MongoDB.Driver;
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Migration
{
    public abstract class DocumentMigration
    {
        // Properties.
        public abstract IRepository SourceRepository { get; }

        // Methods.
        /// <summary>
        /// Perform migration with optional updating callback
        /// </summary>
        /// <param name="callbackEveryTotDocuments">Interval of processed documents between callback invokations. 0 if ignore callback</param>
        /// <param name="callbackAsync">The async callback function. Parameter is number of processed documents</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The migration result</returns>
        public abstract Task<MigrationResult> MigrateAsync(int callbackEveryTotDocuments = 0, Func<long, Task>? callbackAsync = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Migrate documents of a collection
    /// </summary>
    /// <typeparam name="TModel">The model type</typeparam>
    /// <typeparam name="TKey">The model's key type</typeparam>
    public class DocumentMigration<TModel, TKey> : DocumentMigration
        where TModel : class, IEntityModel<TKey>
    {
        // Fields.
        private readonly IRepository<TModel, TKey> _sourceRepository;
        private readonly Func<TModel, IRepository?> destinationRepositorySelector;
        private readonly Func<TModel, object> modelConverter;

        // Constructors.
        public DocumentMigration(IRepository<TModel, TKey> repository)
            : this(repository, repository, m => m)
        { }

        public DocumentMigration(
            IRepository<TModel, TKey> sourceRepository,
            IRepository destinationRepository,
            Func<TModel, object> modelConverter)
            : this(sourceRepository, _ => destinationRepository, modelConverter)
        { }

        public DocumentMigration(
            IRepository<TModel, TKey> sourceRepository,
            Func<TModel, IRepository?> destinationRepositorySelector,
            Func<TModel, object> modelConverter)
        {
            _sourceRepository = sourceRepository ?? throw new ArgumentNullException(nameof(sourceRepository));
            this.destinationRepositorySelector = destinationRepositorySelector ?? throw new ArgumentNullException(nameof(destinationRepositorySelector));
            this.modelConverter = modelConverter ?? throw new ArgumentNullException(nameof(modelConverter));
        }

        // Properties.
        public override IRepository SourceRepository => _sourceRepository;

        // Methods.
        public override Task<MigrationResult> MigrateAsync(
            int callbackEveryTotDocuments = 0,
            Func<long, Task>? callbackAsync = null,
            CancellationToken cancellationToken = default) =>
            _sourceRepository.AccessToCollectionAsync(async sourceCollection =>
            {
                var totMigratedDocuments = 0L;
                try
                {
                    if (callbackEveryTotDocuments < 0)
                        throw new ArgumentOutOfRangeException(nameof(callbackEveryTotDocuments), "Value can't be negative");

                    // Migrate documents.
                    await sourceCollection.Find(FilterDefinition<TModel>.Empty, new FindOptions { NoCursorTimeout = true })
                        .ForEachAsync(async model =>
                        {
                            var destinationRepository = destinationRepositorySelector(model);

                            // Verify if needs to skip this model.
                            if (destinationRepository is null)
                                return;

                            // Replace if it's the same collection, insert one otherwise.
                            if (SourceRepository == destinationRepository)
                                await destinationRepository.ReplaceAsync(model, updateDependentDocuments: false).ConfigureAwait(false);
                            else
                                await destinationRepository.CreateAsync(modelConverter(model)).ConfigureAwait(false);

                            // Increment counter.
                            totMigratedDocuments++;

                            // Execute callback.
                            if (callbackEveryTotDocuments > 0 &&
                                    totMigratedDocuments % callbackEveryTotDocuments == 0 &&
                                    callbackAsync != null)
                                await callbackAsync(totMigratedDocuments).ConfigureAwait(false);

                        }, cancellationToken).ConfigureAwait(false);

                    return MigrationResult.Succeeded(totMigratedDocuments);
                }
                catch (Exception e)
                {
                    return MigrationResult.Failed(totMigratedDocuments, e);
                }
            });
    }
}
