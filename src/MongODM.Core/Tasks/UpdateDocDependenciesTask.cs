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

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.IO;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Driver;
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.FieldDefinition;
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Utility;
using Microsoft.Extensions.Logging;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Tasks
{
    public class UpdateDocDependenciesTask : IUpdateDocDependenciesTask
    {
        // Fields.
        private readonly ILogger<UpdateDocDependenciesTask> logger;
        private readonly ISerializerModifierAccessor serializerModifierAccessor;
        private readonly IServiceProvider serviceProvider;

        // Constructors.
        public UpdateDocDependenciesTask(
            ILogger<UpdateDocDependenciesTask> logger,
            ISerializerModifierAccessor serializerModifierAccessor,
            IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serializerModifierAccessor = serializerModifierAccessor;
            this.serviceProvider = serviceProvider;
        }

        // Methods.
        public async Task RunAsync<TDbContext>(
            string referencedRepositoryName,
            object referencedModelId,
            IEnumerable<string> idMemberMapIdentifiers)
            where TDbContext : class, IDbContext
        {
            if (idMemberMapIdentifiers is null)
                throw new ArgumentNullException(nameof(idMemberMapIdentifiers));
            if (referencedModelId is null)
                throw new ArgumentNullException(nameof(referencedModelId));

            logger.UpdateDocDependenciesTaskStarted(typeof(TDbContext), referencedRepositoryName, referencedModelId.ToString(), idMemberMapIdentifiers);

            // Get data.
            var dbContext = (TDbContext)serviceProvider.GetService(typeof(TDbContext));
            using var dbExecutionContext = new DbExecutionContextHandler(dbContext); //run into a db execution context

            var referencedRepository = dbContext.RepositoryRegistry.Repositories.First(r => r.Name == referencedRepositoryName);
            var referencedModel = await referencedRepository.FindOneAsync(referencedModelId);
            var referencedModelType = dbContext.ProxyGenerator.PurgeProxyType(referencedModel.GetType());

            // Recover reference id member maps model's schemas, and all model maps.
            /*
             * At this point idMemberMapIdentifiers contains Ids from all reference Member Maps, also from different ModelMaps/Schemas, also ponting to the same Id paths.
             * Anyway, we know the referenced model type, and only member maps from the same type are valid. We can select only them.
             * 
             * Verify that member map exists, because a scheduled task could be executed with a different configuration respectly to when it has been generated.
             * This could happen for example if the software is upgraded in the meanwhile.
             */
            var idMemberMaps = idMemberMapIdentifiers
                .Select(idMemberMapIdentifier => dbContext.MapRegistry.MemberMapsById.TryGetValue(idMemberMapIdentifier, out var idmm) ? idmm : null!)
                .Where(idMemberMap => idMemberMap is not null && idMemberMap.ModelMapSchema.ModelMap.ModelType == referencedModelType);

            // Define mapping of serialized documents.
            /*
             * We need to create this dictionary map:
             * - repositoryDictionary: repository -> id member map -> serialized document
             * 
             * Each document is serialized with its current active schema serializer.
             * 
             * Different id paths may share also same serializers. 
             * Because of this, we use an external cache for avoid to serialize multiple times with same serializer.
             */
            var serializedDocumentsCache = new Dictionary<IBsonSerializer, BsonDocument>();
            var repositoryDictionary = idMemberMaps
                .GroupBy(idmm => dbContext.RepositoryRegistry.GetRepositoryByHandledModelType(idmm.MemberMapPath.First().ModelMapSchema.ModelMap.ModelType))
                .ToDictionary(repoGroup => repoGroup.Key,
                              repoGroup => repoGroup
                    .Select(idmm =>
                    {
                        //select active schema serializer
                        var documentSerializer = idmm.ModelMapSchema.ModelMap.ActiveSchema.Serializer;

                        //use cache
                        if (!serializedDocumentsCache.ContainsKey(documentSerializer))
                        {
                            var serializedDocument = new BsonDocument();
                            using var bsonWriter = new BsonDocumentWriter(serializedDocument);
                            var context = BsonSerializationContext.CreateRoot(bsonWriter);
                            documentSerializer.Serialize(context, referencedModel);
                            serializedDocumentsCache[documentSerializer] = serializedDocument;
                        }
                        return (idMemberMap: idmm, doc: serializedDocumentsCache[documentSerializer]);
                    })
                    //take one id member map for each generated path. Drop equivalent member maps generated by secondary schemas, but with same path
                    .GroupBy(pair => pair.idMemberMap.GetElementPath(_ => ".$"))
                    //take idmm with longer active schemas sequence in path.
                    .Select(pathGroup => pathGroup.Aggregate(
                        (default(ValueTuple<IMemberMap, BsonDocument>), -1), //starting value for longest active schema sequence from root
                        (accumulator, newPair) =>
                        {
                            var prevBestLength = accumulator.Item2;
                            var memberMap = newPair.idMemberMap;
                            var activeSchemeSequenceLength = memberMap.MemberMapPath.TakeWhile(mm => mm.ModelMapSchema.IsCurrentActive).Count();
                            return activeSchemeSequenceLength > prevBestLength ?
                                (newPair, activeSchemeSequenceLength) :
                                accumulator;
                        },
                        accumulator => accumulator.Item1))
                    .ToDictionary(pair => pair.Item1, pair => pair.Item2));

            // Find Ids of documents that may need to be updated.
            /*
             * Use all Id paths to find all Ids of existing documents that may need to be updated.
             * Only already existing documents may require an update, so to limit actions on these documents is safe.
             * 
             * This permits to execute FindAndUpdate actions to an enumerable set of documents.
             */
            var updatableDocumentsIdByRepository = new Dictionary<IRepository, IEnumerable<object>>();
            foreach (var repositoryGroup in repositoryDictionary)
            {
                var repository = repositoryGroup.Key;
                var selectedIdMemberMaps = repositoryGroup.Value.Keys;

                var originModelType = repository.ModelType;
                var originIdType = repository.KeyType;

                var result = typeof(UpdateDocDependenciesTask).GetMethod(nameof(FindUpdatableDocumentsIdAsync), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(originModelType, originIdType)
                    .Invoke(null, new[] { repository, selectedIdMemberMaps, referencedModelId });

                updatableDocumentsIdByRepository.Add(repository, await (Task<IEnumerable<object>>)result);
            }

            // Update models.
            /*
             * Update one document at time using FindOneAndUpdate.
             * Iterate on repositories, updatable documents, and Id member maps on different paths.
             */
            foreach (var repoPair in repositoryDictionary)
            {
                var repository = repoPair.Key;

                var originModelType = repository.ModelType;
                var originIdType = repository.KeyType;
                var findAndUpdateAsyncMethodInfo = typeof(UpdateDocDependenciesTask)
                    .GetMethod(nameof(FindAndUpdateAsync), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(originModelType, originIdType);

                foreach (var updatableDocumentId in updatableDocumentsIdByRepository[repository])
                {
                    foreach (var memberMapPair in repoPair.Value)
                    {
                        var idMemberMap = memberMapPair.Key;
                        var serializedDocument = memberMapPair.Value;

                        // Invoke find and update function.
                        var result = findAndUpdateAsyncMethodInfo.Invoke(null, new object[]
                        {
                            repository,
                            idMemberMap,
                            serializedDocument,
                            updatableDocumentId,
                            referencedModelId
                        });
                    }
                }
            }

            logger.UpdateDocDependenciesTaskEnded(typeof(TDbContext), referencedRepositoryName, referencedModelId.ToString());
        }

        // Helpers.
        private static async Task<bool> FindAndUpdateAsync<TOriginModel, TOriginKey>(
            IRepository<TOriginModel, TOriginKey> repository,
            IMemberMap idMemberMap,
            BsonDocument updatedSubDocument,
            TOriginKey originModelId,
            object referencedModelId)
            where TOriginModel : class, IEntityModel<TOriginKey>
        {
            var subDocumentMemberMap = idMemberMap.ParentMemberMap!;

            // Define find filter.
            var filter = Builders<TOriginModel>.Filter.And(new[]
            {
                Builders<TOriginModel>.Filter.Eq(m => m.Id, originModelId),
                BuildFindFilterHelper<TOriginModel, object>(idMemberMap.MemberMapPath, referencedModelId),
            });

            // Define update operator.
            var lastArrayMemberMap = subDocumentMemberMap.MemberMapPath.Reverse().FirstOrDefault(mm => mm.IsSerializedAsArray);
            var update = Builders<TOriginModel>.Update.Set(
                new MemberMapFieldDefinition<TOriginModel, BsonDocument>(
                    subDocumentMemberMap,
                    mm =>
                    {
                        var sb = new StringBuilder();
                        var maxArrayItemDepth = mm.MaxArrayItemDepth;

                        for (int i = 0; i < maxArrayItemDepth; i++)
                        {
                            if (mm == lastArrayMemberMap && //if this is the last array member map
                                i + 1 == maxArrayItemDepth) //and this is max item depth for this array
                                sb.Append(".$[idfilter]"); //filter in array items
                            else
                                sb.Append(".$[]"); //select all array items
                        }
                        return sb.ToString();
                    }),
                updatedSubDocument);

            var arrayFilters = new List<ArrayFilterDefinition>();
            if (lastArrayMemberMap is not null)
                arrayFilters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument($"idfilter.{string.Join(".",
                        idMemberMap.MemberMapPath.Reverse()
                                                 .TakeWhile(mm => mm != lastArrayMemberMap)
                                                 .Reverse()
                                                 .Select(mm => mm.BsonMemberMap.ElementName))}",
                        new BsonDocument("$eq", updatedSubDocument.GetValue(idMemberMap.BsonMemberMap.ElementName)))));

            // Exec update.
            var model = await repository.AccessToCollectionAsync(collection =>
                collection.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<TOriginModel> { ArrayFilters = arrayFilters }));

            return model is not null;
        }

        private static async Task<IEnumerable<object>> FindUpdatableDocumentsIdAsync<TOriginModel, TOriginKey>(
            IRepository<TOriginModel, TOriginKey> repository,
            IEnumerable<IMemberMap> idMemberMaps,
            object referencedModelId)
            where TOriginModel : class, IEntityModel<TOriginKey>
        {
            var cursor = await repository.FindAsync(
                Builders<TOriginModel>.Filter.Or(
                    idMemberMaps.Select(idmm => BuildFindFilterHelper<TOriginModel, object>(idmm.MemberMapPath, referencedModelId))),
                new FindOptions<TOriginModel, TOriginModel>
                {
                    NoCursorTimeout = true,
                    Projection = Builders<TOriginModel>.Projection.Include(m => m.Id) //we need only Id
                }).ConfigureAwait(false);

            List<object> ids = new();
            while (await cursor.MoveNextAsync())
                ids.AddRange(cursor.Current.Select(m => (object)m.Id!));

            return ids;
        }

        private static FilterDefinition<TModel> BuildFindFilterHelper<TModel, TField>(
            IEnumerable<IMemberMap> memberMapPath,
            TField value)
        {
            foreach (var (mm, i) in memberMapPath.Select((mm, i) => (mm, i)))
                if (mm.IsSerializedAsArray)
                    return BuildFindFilterElemMatchHelper(
                        new MemberMapFieldDefinition<TModel>(memberMapPath.ElementAt(i)),
                        memberMapPath.Skip(i + 1),
                        mm.MaxArrayItemDepth,
                        value);

            var lastMemberMap = memberMapPath.Last();
            var elementsToSkip = lastMemberMap.MemberMapPath.Count() - memberMapPath.Count();
            return Builders<TModel>.Filter.Eq(
                new MemberMapFieldDefinition<TModel, TField>(
                    lastMemberMap,
                    skipElementsInPath: elementsToSkip),
                value);
        }

        private static FilterDefinition<TModel> BuildFindFilterElemMatchHelper<TModel, TField>(
            FieldDefinition<TModel>? currentFieldDefinition,
            IEnumerable<IMemberMap> memberMapPath,
            int itemDepth,
            TField value)
        {
            if (itemDepth > 0)
            {
                /* We must build the elemMatch item type considering the itemDepth at this level.
                 * For example, if itemDepth == 1, the item type will be simply TItem.
                 * If itemDepth == 2, the item type will be IEnumerable<TItem>.
                 * If itemDepth == 3, the item type will be IEnumerable<IEnumerable<TItem>>, and so on.
                 */
                var elemMatchItemType = memberMapPath.First().ModelMapSchema.ModelMap.ModelType;
                for (int i = 1; i < itemDepth; i++)
                    elemMatchItemType = typeof(IEnumerable<>).MakeGenericType(elemMatchItemType);

                var genericElemMatchBuilderMethod = typeof(UpdateDocDependenciesTask).GetMethod(nameof(GenericElemMatchBuilderHelper), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(
                        typeof(TModel),
                        elemMatchItemType,
                        typeof(TField));

                return (FilterDefinition<TModel>)genericElemMatchBuilderMethod.Invoke(null,
                    new object[]
                    {
                        currentFieldDefinition!,
                        itemDepth,
                        memberMapPath,
                        value!
                    });
            }
            else return BuildFindFilterHelper<TModel, TField>(memberMapPath, value);
        }

        private static FilterDefinition<TModel> GenericElemMatchBuilderHelper<TModel, TItem, TField>(
            FieldDefinition<TModel>? currentFieldDefinition,
            int itemDepth,
            IEnumerable<IMemberMap> memberMapPath,
            TField value) =>
            currentFieldDefinition is not null ?
                Builders<TModel>.Filter.ElemMatch(
                    currentFieldDefinition,
                    BuildFindFilterElemMatchHelper<TItem, TField>(null, memberMapPath, itemDepth - 1, value)) :
                Builders<TModel>.Filter.ElemMatch(
                    BuildFindFilterElemMatchHelper<TItem, TField>(null, memberMapPath, itemDepth - 1, value));
    }
}
