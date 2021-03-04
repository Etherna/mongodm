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
using Etherna.MongODM.Core.Serialization.Modifiers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Tasks
{
    public class UpdateDocDependenciesTask : IUpdateDocDependenciesTask
    {
        // Fields.
        private readonly ISerializerModifierAccessor serializerModifierAccessor;
        private readonly IServiceProvider serviceProvider;

        // Constructors.
        public UpdateDocDependenciesTask(
            ISerializerModifierAccessor serializerModifierAccessor,
            IServiceProvider serviceProvider)
        {
            this.serializerModifierAccessor = serializerModifierAccessor;
            this.serviceProvider = serviceProvider;
        }

        // Methods.
        public async Task RunAsync<TDbContext, TModel, TKey>(
            IEnumerable<string> idPaths,
            TKey modelId)
            where TModel : class, IEntityModel<TKey>
            where TDbContext : class, IDbContext
        {
            if (idPaths is null)
                throw new ArgumentNullException(nameof(idPaths));

            var dbContext = (TDbContext)serviceProvider.GetService(typeof(TDbContext));

            // Get repository.
            /* Ignore document update if doesn't exists a collection that can handle its type. */
            if (!dbContext.RepositoryRegister.ModelCollectionRepositoryMap.ContainsKey(typeof(TModel)))
                return;

            var repository = (ICollectionRepository<TModel, TKey>)dbContext.RepositoryRegister.ModelCollectionRepositoryMap[typeof(TModel)];

            // Update models.
            HashSet<TKey> upgradedDocumentsId = new();
            using (serializerModifierAccessor.EnableReferenceSerializerModifier(true))
            using (serializerModifierAccessor.EnableCacheSerializerModifier(true))
            {
                foreach (var idPath in idPaths)
                {
                    using var cursor = await repository.FindAsync(
                        Builders<TModel>.Filter.Eq(idPath, modelId),
                        new FindOptions<TModel, TModel> { NoCursorTimeout = true }).ConfigureAwait(false);

                    // Load and replace.
                    while (await cursor.MoveNextAsync().ConfigureAwait(false))
                    {
                        foreach (var model in cursor.Current)
                        {
                            if (!upgradedDocumentsId.Contains(model.Id))
                            {
                                try
                                {
                                    // Replace on db.
                                    await repository.ReplaceAsync(model, false).ConfigureAwait(false);
                                }
                                catch { }

                                // Add id to upgraded list.
                                upgradedDocumentsId.Add(model.Id);
                            }
                        }
                    }
                }
            }
        }
    }
}
