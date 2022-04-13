using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Serialization.Serializers;
using System;
using System.Globalization;
using System.Reflection;

namespace Etherna.MongODM.Core.Serialization.Providers
{
    public class ModelMapSerializationProvider : BsonSerializationProviderBase
    {
        // Fields.
        private readonly DbContext dbContext;

        // Constructor.
        public ModelMapSerializationProvider(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Methods.
        public override IBsonSerializer? GetSerializer(Type type, IBsonSerializerRegistry serializerRegistry)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType && typeInfo.ContainsGenericParameters)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "Generic type {0} has unassigned type parameters.", BsonUtils.GetFriendlyTypeName(type));
                throw new ArgumentException(message, nameof(type));
            }

            if ((typeInfo.IsClass || (typeInfo.IsValueType && !typeInfo.IsPrimitive)) &&
                !typeof(Array).GetTypeInfo().IsAssignableFrom(type) &&
                !typeof(Enum).GetTypeInfo().IsAssignableFrom(type))
            {
                var modelMapSerializerDefinition = typeof(ModelMapSerializer<>);
                var modelMapSerializerType = modelMapSerializerDefinition.MakeGenericType(type);
                return (IBsonSerializer)Activator.CreateInstance(modelMapSerializerType, dbContext);
            }

            return null;
        }
    }
}
