using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace Etherna.MongODM.Operations.ModelMaps
{
    class OperationBaseMap : IModelMapsCollector
    {
        public void Register(IDbContext dbContext)
        {
            dbContext.DocumentSchemaRegister.RegisterModelSchema(
                "0.20.0", //mongodm library's version
                cm =>
                {
                    cm.AutoMap();

                    // Set Id representation.
                    cm.IdMemberMap.SetSerializer(new StringSerializer(BsonType.ObjectId))
                                  .SetIdGenerator(new StringObjectIdGenerator());
                },
                initCustomSerializer: () =>
                    new ExtendedClassMapSerializer<OperationBase>(
                        dbContext.DbCache,
                        dbContext.LibraryVersion,
                        dbContext.SerializerModifierAccessor)
                    { AddVersion = true });
        }
    }
}
