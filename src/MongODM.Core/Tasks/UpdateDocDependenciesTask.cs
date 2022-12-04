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
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Serialization.Serializers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var referencedModel = referencedRepository.FindOneAsync(referencedModelId);

            //recover reference id member maps from all schemas, from all model maps
            var idMemberMaps = idMemberMapIdentifiers.Select(
                idMemberMapIdentifier => dbContext.SchemaRegistry.MemberMapsDictionary[idMemberMapIdentifier]);

            // Extract mapped serializers for each id member.
            /*
             * At this point idMemberMaps contains all Id Member Maps, also from different Schemas/ModelMaps, and also ponting to same Id paths.
             * We need to select for each id path all available unique versions of serializers.
             * If serializer is not an IReferenceSerializer, ignore it and eventually update document with only minimal Id document.
             */
            var idPathSerializersDictionary = idMemberMaps
                .GroupBy(idmm => idmm.DefinitionPath.ElementPathAsString)
                .ToDictionary(group => group.Key, group => group.Select(idmm => (idmm.OwnerModelMap.BsonClassMapSerializer as IReferenceSerializer)!)
                                                                .Where(serializer => serializer != null)
                                                                .Distinct());

            // Serialize referenced document for each available serializer type.
            var documentsBySerializerDictionary = idPathSerializersDictionary.Values
                .SelectMany(list => list)
                .Distinct()
                .ToDictionary(
                    serializer => serializer,
                    serializer =>
                    {
                        var serializedReferenceDocument = new BsonDocument();
                        using var bsonWriter = new BsonDocumentWriter(serializedReferenceDocument);
                        var context = BsonSerializationContext.CreateRoot(bsonWriter);

                        serializer.Serialize(context, referencedModel);

                        return serializedReferenceDocument;
                    });

            // Find all origin repositories.
            var originRepositories = idMemberMaps
                .Select(idmm => dbContext.RepositoryRegistry.GetRepositoryByHandledModelType(idmm.RootModelMap.ModelType))
                .Distinct();

            foreach (var originRepository in originRepositories)
            {
                // Find ids of origin documents that need to be updated.
                var upgradableDocsCursor = await originRepository.FindAsync(
                    Builders<TOriginModel>.Filter.Or(
                        idPaths.Select(idPath => Builders<TOriginModel>.Filter.Eq(idPath, referencedModelId))),
                    new FindOptions<TOriginModel, TOriginModel>
                    {
                        NoCursorTimeout = true,
                        Projection = Builders<TOriginModel>.Projection.Include(m => m.Id) //we need only Id
                    }).ConfigureAwait(false);
            }







            // Prepare serialized sub-documents.
            var serializedDocumentsCache = new Dictionary<string, BsonDocument>(); // id path -> serialized sub-documents
            foreach (var idMemberMap in idMemberMaps)
            {

                // Select serializers.

                var referenceSerializer = idMemberMap.OwnerModelMap.Serializer;

                var serializedReferenceDocument = new BsonDocument();
                using var bsonWriter = new BsonDocumentWriter(serializedReferenceDocument);
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                if (referenceSerializer is not null) //if property is defined in ActiveMap, use its serializer
                {
                    referenceSerializer.Serialize(context, referencedModel);
                }
                else //if is not defined, serialize as minimal reference with only id
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteName(referencedModelActiveMap.BsonClassMap.IdMemberMap.ElementName);
                    referencedModelActiveMap.BsonClassMap.IdMemberMap.GetSerializer().Serialize(context, referencedModelId);
                    bsonWriter.WriteEndDocument();
                }

                serializedDocumentsCache[idMemberMap] = serializedReferenceDocument;
            }

            // Update models.
            //find ids of documents that need to be updated
            var upgradableDocsCursor = await originRepository.FindAsync(
                Builders<TOriginModel>.Filter.Or(
                    idPaths.Select(idPath => Builders<TOriginModel>.Filter.Eq(idPath, referencedModelId))),
                new FindOptions<TOriginModel, TOriginModel>
                {
                    NoCursorTimeout = true,
                    Projection = Builders<TOriginModel>.Projection.Include(m => m.Id) //we need only Id
                }).ConfigureAwait(false);

            //update one document at time
            while (await upgradableDocsCursor.MoveNextAsync().ConfigureAwait(false))
                foreach (var upgradableDocId in upgradableDocsCursor.Current.Select(m => m.Id))
                {
                    //find model map Id and related ModelMap


                    /* Process one Id path per time.
                     * This is required, because we need to verify when we "find-update" a document, that the property is still reporting the right document reference.
                     * If the source document has changed in the meantime, and we doesn't check this with id path granularity, we could create inconsistence states.
                     */
                    foreach (var idPath in idPaths)
                    {

                    }
                }

            logger.UpdateDocDependenciesTaskEnded(dbContext.Options.DbName, typeof(TModel).Name, modelId.ToString());
        }
    }
}
