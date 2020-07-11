﻿using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Serialization.Serializers
{
    public class EnumerableSerializer<TItem> :
        EnumerableSerializerBase<IEnumerable<TItem>, TItem>,
        IChildSerializerConfigurable,
        IReferenceContainerSerializer
    {
        // Constructors.
        /// <summary>
        /// Initializes a new instance of the <see cref="StackSerializer{TItem}"/> class.
        /// </summary>
        public EnumerableSerializer()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackSerializer{TItem}"/> class.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        public EnumerableSerializer(IBsonSerializer<TItem> itemSerializer)
            : base(itemSerializer)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackSerializer{TItem}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public EnumerableSerializer(IBsonSerializerRegistry serializerRegistry)
            : base(serializerRegistry)
        { }

        // Properties.
        public IBsonSerializer ChildSerializer => ItemSerializer;

        public IEnumerable<BsonClassMap> ContainedClassMaps =>
            ItemSerializer is IClassMapContainerSerializer classMapContainer ?
            classMapContainer.ContainedClassMaps : Array.Empty<BsonClassMap>();

        public bool? UseCascadeDelete =>
            (ItemSerializer as IReferenceContainerSerializer)?.UseCascadeDelete;

        // Public methods.
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IEnumerable<TItem> value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            // Force to exclude enumerable actual type from serialization.
            args = new BsonSerializationArgs(value.GetType(), true, args.SerializeIdFirst);

            base.Serialize(context, args, value);
        }

        public IBsonSerializer WithChildSerializer(IBsonSerializer childSerializer) =>
            WithItemSerializer((IBsonSerializer<TItem>)childSerializer);

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified item serializer.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public EnumerableSerializer<TItem> WithItemSerializer(IBsonSerializer<TItem> itemSerializer)
        {
            return new EnumerableSerializer<TItem>(itemSerializer);
        }

        // Protected methods.
        protected override void AddItem(object accumulator, TItem item)
        {
            if (accumulator is null)
                throw new ArgumentNullException(nameof(accumulator));

            ((List<TItem>)accumulator).Add(item);
        }

        protected override object CreateAccumulator() =>
            new List<TItem>();

        protected override IEnumerable<TItem> EnumerateItemsInSerializationOrder(IEnumerable<TItem> value) =>
            value;

        protected override IEnumerable<TItem> FinalizeResult(object accumulator) =>
            (IEnumerable<TItem>)accumulator;
    }
}
