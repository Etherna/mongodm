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

using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Bson.Serialization.Options;
using Etherna.MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class DictionarySerializer<TKey, TValue> :
        DictionarySerializerBase<IDictionary<TKey, TValue>, TKey, TValue>,
        IChildSerializerConfigurable,
        IReferenceContainerSerializer,
        IDictionaryRepresentationConfigurable<DictionarySerializer<TKey, TValue>>
    {
        // Constructors.
        public DictionarySerializer()
        { }

        public DictionarySerializer(DictionaryRepresentation dictionaryRepresentation)
            : base(dictionaryRepresentation)
        { }

        public DictionarySerializer(DictionaryRepresentation dictionaryRepresentation, IBsonSerializerRegistry serializerRegistry)
            : base(dictionaryRepresentation, serializerRegistry)
        { }

        public DictionarySerializer(DictionaryRepresentation dictionaryRepresentation, IBsonSerializer<TKey> keySerializer, IBsonSerializer<TValue> valueSerializer)
            : base(dictionaryRepresentation, keySerializer, valueSerializer)
        { }

        // Properties.
        public IEnumerable<BsonClassMap> AllChildClassMaps =>
            (ValueSerializer as IModelMapsContainerSerializer)?.AllChildClassMaps ??
            Array.Empty<BsonClassMap>();

        public IBsonSerializer ChildSerializer => ValueSerializer;
        
        public bool UseCascadeDelete =>
            (ValueSerializer as IReferenceContainerSerializer)?.UseCascadeDelete ?? false;

        // Public methods.
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IDictionary<TKey, TValue> value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            // Force to exclude enumerable actual type from serialization.
            args = new BsonSerializationArgs(value.GetType(), true, args.SerializeIdFirst);

            base.Serialize(context, args, value);
        }

        public IBsonSerializer WithChildSerializer(IBsonSerializer childSerializer) =>
            WithValueSerializer((IBsonSerializer<TValue>)childSerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified dictionary representation.
        /// </summary>
        /// <param name="dictionaryRepresentation">The dictionary representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public DictionarySerializer<TKey, TValue> WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation) =>
            dictionaryRepresentation == DictionaryRepresentation ?
                this :
                new DictionarySerializer<TKey, TValue>(dictionaryRepresentation, KeySerializer, ValueSerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified dictionary representation and key value serializers.
        /// </summary>
        /// <param name="dictionaryRepresentation">The dictionary representation.</param>
        /// <param name="keySerializer">The key serializer.</param>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public DictionarySerializer<TKey, TValue> WithDictionaryRepresentation(
            DictionaryRepresentation dictionaryRepresentation,
            IBsonSerializer<TKey> keySerializer,
            IBsonSerializer<TValue> valueSerializer) =>
            dictionaryRepresentation == DictionaryRepresentation && keySerializer == KeySerializer && valueSerializer == ValueSerializer ?
                this :
                new DictionarySerializer<TKey, TValue>(dictionaryRepresentation, keySerializer, valueSerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified key serializer.
        /// </summary>
        /// <param name="keySerializer">The key serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public DictionarySerializer<TKey, TValue> WithKeySerializer(IBsonSerializer<TKey> keySerializer) =>
            keySerializer == KeySerializer ?
                this :
                new DictionarySerializer<TKey, TValue>(DictionaryRepresentation, keySerializer, ValueSerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified value serializer.
        /// </summary>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public DictionarySerializer<TKey, TValue> WithValueSerializer(IBsonSerializer<TValue> valueSerializer) =>
            valueSerializer == ValueSerializer ?
                this :
                new DictionarySerializer<TKey, TValue>(DictionaryRepresentation, KeySerializer, valueSerializer);

        // Protected methods.
        protected override ICollection<KeyValuePair<TKey, TValue>> CreateAccumulator() =>
            new Dictionary<TKey, TValue>();
        
        // Explicit interface implementations.
        IBsonSerializer IDictionaryRepresentationConfigurable.WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation) =>
            WithDictionaryRepresentation(dictionaryRepresentation);
    }
}
