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
using Etherna.MongODM.Core.Repositories;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
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
            var referencedRepository = dbContext.RepositoryRegistry.Repositories.First(r => r.Name == referencedRepositoryName);
            var referencedModel = await referencedRepository.FindOneAsync(referencedModelId);

            // Recover reference id member maps model's schemas, and all model maps.
            /*
             * At this point idMemberMapIdentifiers contains Ids from all reference Member Maps, also from different ModelMaps/Schemas, also ponting to the same Id paths.
             * Anyway, we know the referenced model type, and only member maps from the same type schema are valid. We can select only them.
             * All model maps from this schema will be searched for updates.
             * 
             * If the reference summary had a different type, the proper id map will not be find.
             * In this case, replace summary with minimal reference with only Id.
             */
            var idMemberMaps = idMemberMapIdentifiers
                .Select(idMemberMapIdentifier => dbContext.MapRegistry.MemberMapsById[idMemberMapIdentifier])
                .Where(idMemberMap => idMemberMap.ModelMapSchema.ModelMap.ModelType == referencedModel.GetType());

            // Define mapping of serializers and serialized documents.
            /*
             * We need to create two dictionary maps:
             * - repositoryDictionary: repository -> id find strings -> serializer[]
             * - serializedDocumentDictionary: serializer -> serializated document
             * 
             * This permits to denormalize and optimize the mapping from each id path to all their possible serialized documents.
             */
            var repositoryDictionary = idMemberMaps
                .GroupBy(idmm => dbContext.RepositoryRegistry.GetRepositoryByHandledModelType(idmm.ModelMapSchema.ModelType))
                .ToDictionary(repoGroup => repoGroup.Key,
                              repoGroup => repoGroup.GroupBy(idmm => MemberMapToMongoFindString(idmm))
                                                    .ToDictionary(idFindStringGroup => idFindStringGroup.Key,
                                                                  idFindStringGroup => idFindStringGroup.Select(idmm => idmm.ModelMapSchema.Serializer)));
            var serializedDocumentDictionary = repositoryDictionary
                .SelectMany(repoPair => repoPair.Value)
                .SelectMany(idFindStringPair => idFindStringPair.Value)
                .ToDictionary(serializer => serializer,
                              serializer =>
                              {
                                  var serializedDocument = new BsonDocument();
                                  using var bsonWriter = new BsonDocumentWriter(serializedDocument);
                                  var context = BsonSerializationContext.CreateRoot(bsonWriter);
                                  serializer.Serialize(context, referencedModel);
                                  return serializedDocument;
                              });

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
             * Iterate on repositories, available reference Ids on each repository, and available serializer for each Id.
             */
            foreach (var repoPair in repositoryDictionary)
            {
                var repository = repoPair.Key;
                var idFindStringDictionary = repoPair.Value;
                var updatableDocumentsId = updatableDocumentsIdByRepository[repository];

                foreach (var idFindStringPair in idFindStringDictionary)
                {
                    var idFindString = idFindStringPair.Key;
                    var serializers = idFindStringPair.Value;

                    foreach (var serializer in serializers)
                    {
                        var serializedDocument = serializedDocumentDictionary[serializer];

                        //TODO.
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

        private static string MemberMapToMongoFindString(IMemberMap memberMap) =>
            string.Join(".", memberMap.DefinitionMemberPath.Select(mm => mm.BsonMemberMap.ElementName));
    }
}
