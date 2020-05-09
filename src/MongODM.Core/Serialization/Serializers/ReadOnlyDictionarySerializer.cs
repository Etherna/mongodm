﻿using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using System.Collections.Generic;

namespace Digicando.MongODM.Serialization.Serializers
{
    public class ReadOnlyDictionarySerializer<TKey, TValue> :
        DictionarySerializerBase<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>,
        IChildSerializerConfigurable,
        IReferenceContainerSerializer,
        IDictionaryRepresentationConfigurable<ReadOnlyDictionarySerializer<TKey, TValue>>
    {
        // Constructors.
        public ReadOnlyDictionarySerializer()
        { }

        public ReadOnlyDictionarySerializer(DictionaryRepresentation dictionaryRepresentation)
            : base(dictionaryRepresentation)
        { }

        public ReadOnlyDictionarySerializer(DictionaryRepresentation dictionaryRepresentation, IBsonSerializerRegistry serializerRegistry)
            : base(dictionaryRepresentation, serializerRegistry)
        { }

        public ReadOnlyDictionarySerializer(DictionaryRepresentation dictionaryRepresentation, IBsonSerializer<TKey> keySerializer, IBsonSerializer<TValue> valueSerializer)
            : base(dictionaryRepresentation, keySerializer, valueSerializer)
        { }

        // Properties.
        public IEnumerable<BsonClassMap> ContainedClassMaps =>
            ValueSerializer is IClassMapContainerSerializer classMapContainer ?
            classMapContainer.ContainedClassMaps : new BsonClassMap[0];
        
        public bool? UseCascadeDelete =>
            (ValueSerializer as IReferenceContainerSerializer)?.UseCascadeDelete;

        // Public methods.
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IReadOnlyDictionary<TKey, TValue> value)
        {
            // Force to exclude enumerable actual type from serialization.
            args = new BsonSerializationArgs(value.GetType(), true, args.SerializeIdFirst);

            base.Serialize(context, args, value);
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified dictionary representation.
        /// </summary>
        /// <param name="dictionaryRepresentation">The dictionary representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public ReadOnlyDictionarySerializer<TKey, TValue> WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation) =>
            dictionaryRepresentation == DictionaryRepresentation ?
                this :
                new ReadOnlyDictionarySerializer<TKey, TValue>(dictionaryRepresentation, KeySerializer, ValueSerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified dictionary representation and key value serializers.
        /// </summary>
        /// <param name="dictionaryRepresentation">The dictionary representation.</param>
        /// <param name="keySerializer">The key serializer.</param>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public ReadOnlyDictionarySerializer<TKey, TValue> WithDictionaryRepresentation(
            DictionaryRepresentation dictionaryRepresentation,
            IBsonSerializer<TKey> keySerializer,
            IBsonSerializer<TValue> valueSerializer) =>
            dictionaryRepresentation == DictionaryRepresentation && keySerializer == KeySerializer && valueSerializer == ValueSerializer ?
                this :
                new ReadOnlyDictionarySerializer<TKey, TValue>(dictionaryRepresentation, keySerializer, valueSerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified key serializer.
        /// </summary>
        /// <param name="keySerializer">The key serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public ReadOnlyDictionarySerializer<TKey, TValue> WithKeySerializer(IBsonSerializer<TKey> keySerializer) =>
            keySerializer == KeySerializer ?
                this :
                new ReadOnlyDictionarySerializer<TKey, TValue>(DictionaryRepresentation, keySerializer, ValueSerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified value serializer.
        /// </summary>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public ReadOnlyDictionarySerializer<TKey, TValue> WithValueSerializer(IBsonSerializer<TValue> valueSerializer) =>
            valueSerializer == ValueSerializer ?
                this :
                new ReadOnlyDictionarySerializer<TKey, TValue>(DictionaryRepresentation, KeySerializer, valueSerializer);

        // Protected methods.
        protected override ICollection<KeyValuePair<TKey, TValue>> CreateAccumulator() =>
            new Dictionary<TKey, TValue>();
        
        // Explicit interface implementations.
        IBsonSerializer IChildSerializerConfigurable.ChildSerializer => ValueSerializer;

        IBsonSerializer IChildSerializerConfigurable.WithChildSerializer(IBsonSerializer childSerializer) =>
            WithValueSerializer((IBsonSerializer<TValue>)childSerializer);

        IBsonSerializer IDictionaryRepresentationConfigurable.WithDictionaryRepresentation(DictionaryRepresentation dictionaryRepresentation) =>
            WithDictionaryRepresentation(dictionaryRepresentation);
    }
}