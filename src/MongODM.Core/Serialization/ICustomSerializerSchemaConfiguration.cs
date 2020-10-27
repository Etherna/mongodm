using MongoDB.Bson.Serialization;

namespace Etherna.MongODM.Core.Serialization
{
    public interface ICustomSerializerSchemaConfiguration : ISchemaConfiguration
    {
        public IBsonSerializer CustomSerializer { get; }
    }

    public interface ICustomSerializerSchemaConfiguration<TModel> : ICustomSerializerSchemaConfiguration
        where TModel : class
    {
    }
}