using Etherna.MongODM.Serialization;

namespace Etherna.MongODM.Models.Internal.ModelMaps
{
    class SeedOperationMap : IModelMapsCollector
    {
        public void Register(IDbContext dbContext)
        {
            dbContext.DocumentSchemaRegister.RegisterModelSchema<SeedOperation>(
                "0.20.0");
        }
    }
}
