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

using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Bson.Serialization.Serializers;
using Etherna.MongODM.Core.Domain.Models;
using Etherna.MongODM.Core.Serialization.Mapping;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class EntityModelSerializerAdapter<TExternalModel, TInternalModel, TKey> :
        SerializerBase<TExternalModel>,
        IBsonSerializer<TExternalModel>,
        IBsonDocumentSerializer,
        IBsonIdProvider,
        IModelMapsContainerSerializer
        where TInternalModel : class, IEntityModel<TKey>
        where TExternalModel : class, IEntityModel<TKey>
    {
        // Fields.
        private readonly IBsonSerializer<TInternalModel> serializerBase;

        // Constructors.
        public EntityModelSerializerAdapter(IBsonSerializer<TInternalModel> serializerBase)
        {
            this.serializerBase = serializerBase;
        }

        // Properties.
        public IEnumerable<IModelMapSchema> AllChildModelMapSchemas => (serializerBase as IModelMapsContainerSerializer)?.AllChildModelMapSchemas ?? Array.Empty<IModelMapSchema>();

        // Methods.
        public override TExternalModel Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            (serializerBase.Deserialize(context, args) as TExternalModel)!;

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            if (serializerBase is IBsonIdProvider idProviderSerializerBase)
                return idProviderSerializerBase.GetDocumentId(document, out id, out idNominalType, out idGenerator);

            id = null!;
            idNominalType = null!;
            idGenerator = null!;
            return false;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TExternalModel value)
        {
            serializerBase.Serialize(context, args, value);
        }

        public void SetDocumentId(object document, object id)
        {
            if (serializerBase is IBsonIdProvider idProviderSerializerBase)
                idProviderSerializerBase.SetDocumentId(document, id);
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            if (serializerBase is IBsonDocumentSerializer documentSerializerBase)
                return documentSerializerBase.TryGetMemberSerializationInfo(memberName, out serializationInfo);

            serializationInfo = null!;
            return false;
        }
    }
}
