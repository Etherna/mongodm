using Etherna.MongODM.ProxyModels;
using MongoDB.Bson.Serialization;

namespace Etherna.MongODM.Serialization.Serializers
{
    public class ProxyTolerantBsonClassMapSerializer<TClass> : BsonClassMapSerializer<TClass>
    {
        // Fields.
        private readonly IProxyGenerator proxyGenerator;

        // Constructors.
        public ProxyTolerantBsonClassMapSerializer(
            BsonClassMap classMap,
            IProxyGenerator proxyGenerator)
            : base(classMap)
        {
            this.proxyGenerator = proxyGenerator;
        }

        // Methods.
        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TClass value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                if (proxyGenerator.PurgeProxyType(actualType) == typeof(TClass))
                {
                    SerializeClass(context, args, value);
                }
                else
                {
                    var serializer = BsonSerializer.LookupSerializer(actualType);
                    serializer.Serialize(context, args, value);
                }
            }
        }
    }
}
