﻿//   Copyright 2020-present Etherna Sagl
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
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
             * All schemas from this model map will be searched for updates.
             * 
             * If the referenced model summary in db documents has a different type than the current model, the proper map ids will not be find.
             * If an Id path can't be find with any model map schema Id, at the end, try to replace summary with a minimal reference with only Id.
             */
            var idMemberMaps = idMemberMapIdentifiers
                .Select(idMemberMapIdentifier => dbContext.MapRegistry.MemberMapsById[idMemberMapIdentifier])
                .Where(idMemberMap => idMemberMap.ModelMapSchema.ModelMap.ModelType == referencedModelType);

            // Define mapping of serialized documents.
            /*
             * We need to create this dictionary map:
             * - repositoryDictionary: repository -> id path -> serialized documents[]
             * 
             * Different id paths may share also same serializers. 
             * Because of this, we use an external cache for avoid to serialize multiple times with same serializer.
             * 
             * But also different serializers can lead to same serialized document, so we select documents and apply a Distinct at last level.
             */
            var serializedDocumentsCache = new Dictionary<IBsonSerializer, BsonDocument>();
            var repositoryDictionary = idMemberMaps
                .GroupBy(idmm => dbContext.RepositoryRegistry.GetRepositoryByHandledModelType(idmm.DefinitionMemberPath.First().ModelMapSchema.ModelMap.ModelType))
                .ToDictionary(repoGroup => repoGroup.Key,
                              repoGroup => repoGroup.GroupBy(idmm => MemberMapToMongoFindMemberPath(idmm))
                                                    .ToDictionary(
                                  idPathGroup => idPathGroup.Key,
                                  idPathGroup => idPathGroup.Select(idmm => idmm.ModelMapSchema.Serializer)
                                                            .Select(serializer =>
                                                            {
                                                                //use cache
                                                                if (!serializedDocumentsCache.ContainsKey(serializer))
                                                                {
                                                                    var serializedDocument = new BsonDocument();
                                                                    using var bsonWriter = new BsonDocumentWriter(serializedDocument);
                                                                    var context = BsonSerializationContext.CreateRoot(bsonWriter);
                                                                    serializer.Serialize(context, referencedModel);
                                                                    serializedDocumentsCache[serializer] = serializedDocument;
                                                                }
                                                                return serializedDocumentsCache[serializer];
                                                            })
                                                            //select only documents having a model map id
                                                            .Where(bsonDoc => bsonDoc.TryGetElement(dbContext.Options.ModelMapVersion.ElementName, out var _))
                                                            .Distinct()));

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
                var idFindStrings = repositoryGroup.Value.Keys;

                var originModelType = repository.ModelType;
                var originIdType = repository.KeyType;

                var result = typeof(UpdateDocDependenciesTask).GetMethod(nameof(FindUpdatableDocumentsIdAsync), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(originModelType, originIdType)
                    .Invoke(null, new[] { repository, idFindStrings, referencedModelId });

                updatableDocumentsIdByRepository.Add(repository, await (Task<IEnumerable<object>>)result);
            }

            // Update models.
            /*
             * Update one document at time using FindOneAndUpdate.
             * Iterate on repositories, Id paths for each repo, existing documents to update.
             * 
             * For each document, try at first all serialized documents available with its model map id.
             * If no one is found, try to search without any model map Id. In this case replace with minimal reference.
             */
            foreach (var repoPair in repositoryDictionary)
            {
                var repository = repoPair.Key;

                foreach (var idPathPair in repoPair.Value)
                {
                    var idPath = idPathPair.Key;

                    foreach (var updatableDocumentId in updatableDocumentsIdByRepository[repository])
                    {
                        var documentIsUpdated = false;

                        // Try to search for serialized documents.
                        foreach (var serializedDocument in idPathPair.Value)
                        {
                            var modelMapIdElement = serializedDocument.GetElement(dbContext.Options.ModelMapVersion.ElementName).Value.AsString;

                            //TODO.
                        }

                        // Try to search without any model map Id.
                        if (!documentIsUpdated)
                        {
                            //TODO.
                        }
                    }
                }
            }

            logger.UpdateDocDependenciesTaskEnded(typeof(TDbContext), referencedRepositoryName, referencedModelId.ToString());
        }

        // Helpers.
        private static async Task<IEnumerable<object>> FindUpdatableDocumentsIdAsync<TOriginModel, TOriginKey>(
            IRepository<TOriginModel, TOriginKey> repository,
            IEnumerable<string> idFindStrings,
            object referencedModelId)
            where TOriginModel : class, IEntityModel<TOriginKey>
        {
            var cursor = await repository.FindAsync(
                Builders<TOriginModel>.Filter.Or(
                    idFindStrings.Select(idFindString => Builders<TOriginModel>.Filter.Eq(idFindString, referencedModelId))),
                new FindOptions<TOriginModel, TOriginModel>
                {
                    NoCursorTimeout = true,
                    Projection = Builders<TOriginModel>.Projection.Include(m => m.Id) //we need only Id
                }).ConfigureAwait(false);

            List<object> ids = new();
            while(await cursor.MoveNextAsync())
                ids.AddRange(cursor.Current.Select(m => (object)m.Id!));

            return ids;
        }

        private static string MemberMapToMongoFindMemberPath(IMemberMap memberMap) =>
            string.Join(".", memberMap.DefinitionMemberPath.Select(mm => mm.BsonMemberMap.MemberName));
    }
}
