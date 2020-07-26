using Etherna.MongODM.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace Etherna.MongODM.AspNetCoreSample.Models.ModelMaps
{
    class ModelBaseMap : IModelMapsCollector
    {
        public void Register(IDbContext dbContext)
        {
            dbContext.DocumentSchemaRegister.RegisterModelSchema<ModelBase>("1.0.0");

            dbContext.DocumentSchemaRegister.RegisterModelSchema<EntityModelBase<string>>("1.0.0",
                modelMap =>
                {
                    modelMap.AutoMap();

                    modelMap.IdMemberMap.SetSerializer(new StringSerializer(BsonType.ObjectId))
                                        .SetIdGenerator(new StringObjectIdGenerator());
                });
        }
    }
}
