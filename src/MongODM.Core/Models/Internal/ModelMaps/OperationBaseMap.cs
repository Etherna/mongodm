using Etherna.MongODM.Serialization;
using Etherna.MongODM.Serialization.Serializers;

namespace Etherna.MongODM.Models.Internal.ModelMaps
{
    class OperationBaseMap : IModelMapsCollector
    {
        public void Register(IDbContext dbContext)
        {
            dbContext.DocumentSchemaRegister.RegisterModelSchema(
                "0.20.0", //mongodm library's version
                cm => cm.AutoMap(),
                initCustomSerializer: () =>
                    new ExtendedClassMapSerializer<OperationBase>(
                        dbContext.DbCache,
                        dbContext.LibraryVersion,
                        dbContext.SerializerModifierAccessor)
                    { AddVersion = true });
        }
    }
}
