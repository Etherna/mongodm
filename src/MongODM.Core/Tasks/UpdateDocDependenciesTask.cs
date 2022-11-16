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
        public async Task RunAsync<TDbContext, TOriginModel, TOriginKey, TReferenceModel, TReferenceKey>(
            IEnumerable<string> idPaths,
            TReferenceKey referencedModelId)
            where TOriginModel : class, IEntityModel<TOriginKey>
            where TReferenceModel : class, IEntityModel<TReferenceKey>
            where TDbContext : class, IDbContext
        {
            /*
             * Documents are not updated to the last active model map, but only properties are updated using FindOneAndUpdate command on specific sub-documents.
             * 
             * Property serializers are selected looking for definition on the current active model map.
             * We can do this, because references are not strictly binded to the current model map version on document, 
             * and in any case, we can assume that current active model map is the best summary for current application.
             * 
             * In case that an IdPath is not mapped on current active map, we can simply update with a minimal id reference.
             * The mapping Id definition is taken from the active map of referenced model, and the sub-document will not have any map Id associated.
             * Consideraton is that in this edge case probably the property is not more necessary, and even if it is, it can always be recovered by lazy loading.
             * 
             * In this way we can also cache serialized sub-documents by IdPath.
             */

            if (idPaths is null)
                throw new ArgumentNullException(nameof(idPaths));
            if (referencedModelId is null)
                throw new ArgumentNullException(nameof(referencedModelId));

            // Get data.
            var dbContext = (TDbContext)serviceProvider.GetService(typeof(TDbContext));
            var originRepository = dbContext.RepositoryRegistry.GetRepositoryByBaseModelType<TOriginModel, TOriginKey>();
            var referencedRepository = dbContext.RepositoryRegistry.GetRepositoryByBaseModelType<TReferenceModel, TReferenceKey>();

            var originModelActiveMap = dbContext.SchemaRegistry.GetModelMapsSchema(typeof(TOriginModel)).ActiveModelMap;
            var referenceModelActiveMap = dbContext.SchemaRegistry.GetModelMapsSchema(typeof(TReferenceModel)).ActiveModelMap;

            var referencedModel = referencedRepository.FindOneAsync(referencedModelId);

            logger.UpdateDocDependenciesTaskStarted(dbContext.Options.DbName, originRepository.Name, referencedRepository.Name, referencedModelId.ToString(), idPaths);

            // Prepare serialized sub-documents.
            var serializedDocumentsCache = new Dictionary<string, BsonDocument>(); // idPath -> serialized sub-documents

            foreach (var idPath in idPaths)
            {
                var referenceSerializer = TryFindReferenceSerializer(originModelActiveMap.BsonClassMap, idPath.Split('.'));

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
                    bsonWriter.WriteName(referenceModelActiveMap.BsonClassMap.IdMemberMap.ElementName);
                    referenceModelActiveMap.BsonClassMap.IdMemberMap.GetSerializer().Serialize(context, referencedModelId);
                    bsonWriter.WriteEndDocument();
                }

                serializedDocumentsCache[idPath] = serializedReferenceDocument;
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

        // Helpers.
        /// <summary>
        /// Try to find recursively the proper ReferenceSerializer exploring bsonClassMap with the given path.
        /// </summary>
        /// <param name="bsonClassMap">The class map to explore</param>
        /// <param name="idPathSplitted">The path to search</param>
        /// <returns>The reference serializer, if exists</returns>
        private static IBsonSerializer? TryFindReferenceSerializer(BsonClassMap bsonClassMap, IEnumerable<string> idPathSplitted)
        {
            // If path is empty or only Id, we don't have a valid condition.
            if (idPathSplitted.Count() <= 1)
                throw new ArgumentException("Invalid id path", nameof(idPathSplitted));

            // Get serializer.
            var memberMap = bsonClassMap.AllMemberMaps.FirstOrDefault(member => member.ElementName == idPathSplitted.First());
            if (memberMap is null) //if sub-document is not mapped
                return null;

            var memberSerializer = memberMap.GetSerializer();

            //if path is "<sub-document>._id", return the serializer
            if (idPathSplitted.Count() == 2)
                return memberSerializer;

            //else, if id path is longer and it is not a model map container, we can't proceed document exploration
            else if (memberSerializer is not IModelMapsContainerSerializer modelMapsContainerMemberSerializer)
                return null;

            //else, proceed with recursion
            else
                return TryFindReferenceSerializer(modelMapsContainerMemberSerializer.ActiveChildBsonClassMap, idPathSplitted.Skip(1));
        }
    }
}
