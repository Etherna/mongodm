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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Serialization.Serializers
{
    class ReferenceSerializerAdapter<TInModel, TOutModel, TKey> :
        SerializerBase<TOutModel>, IBsonSerializer<TOutModel>, IBsonDocumentSerializer, IBsonIdProvider, IReferenceContainerSerializer
        where TInModel : class, IEntityModel<TKey>
        where TOutModel : class, TInModel
    {
        // Fields.
        private readonly ReferenceSerializer<TInModel, TKey> serializerBase;

        // Constructors.
        public ReferenceSerializerAdapter(ReferenceSerializer<TInModel, TKey> serializerBase)
        {
            this.serializerBase = serializerBase;
        }

        // Properties.
        public IEnumerable<BsonClassMap> ContainedClassMaps => serializerBase.ContainedClassMaps;
        public bool? UseCascadeDelete => serializerBase.UseCascadeDelete;

        // Methods.
        public override TOutModel Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return (TOutModel)serializerBase.Deserialize(context, args);
        }

        public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            return serializerBase.GetDocumentId(document, out id, out idNominalType, out idGenerator);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TOutModel value)
        {
            serializerBase.Serialize(context, args, value);
        }

        public void SetDocumentId(object document, object id)
        {
            serializerBase.SetDocumentId(document, id);
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            return serializerBase.TryGetMemberSerializationInfo(memberName, out serializationInfo);
        }
    }
}
