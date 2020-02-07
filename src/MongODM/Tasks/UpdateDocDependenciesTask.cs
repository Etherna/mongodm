using Digicando.MongODM.Models;
using Digicando.MongODM.Repositories;
using Digicando.MongODM.Serialization.Modifiers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Digicando.MongODM.Tasks
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
            var dbContext = serviceProvider.GetService(typeof(TDbContext)) as TDbContext;

            // Get repository.
            if (!dbContext.ModelCollectionRepositoryMap.ContainsKey(typeof(TModel)))
                return;
            var repository = dbContext.ModelCollectionRepositoryMap[typeof(TModel)] as ICollectionRepository<TModel, TKey>;

            HashSet<TKey> upgradedDocumentsId = new HashSet<TKey>();
            using (serializerModifierAccessor.EnableReferenceSerializerModifier(true))
            using (serializerModifierAccessor.EnableCacheSerializerModifier(true))
            {
                foreach (var idPath in idPaths)
                {
                    using (var cursor = await repository.FindAsync(
                        Builders<TModel>.Filter.Eq(idPath, modelId),
                        new FindOptions<TModel, TModel> { NoCursorTimeout = true }))
                    {
                        // Load and replace.
                        while (await cursor.MoveNextAsync())
                            foreach (var model in cursor.Current)
                            {
                                if (!upgradedDocumentsId.Contains(model.Id))
                                {
                                    try
                                    {
                                        // Replace on db.
                                        await repository.ReplaceAsync(model, false);
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
