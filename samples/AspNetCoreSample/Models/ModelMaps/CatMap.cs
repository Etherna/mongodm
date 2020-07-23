using Etherna.MongODM.Serialization;

namespace Etherna.MongODM.AspNetCoreSample.Models.ModelMaps
{
    class CatMap : IModelMapsCollector
    {
        public void Register(IDbContext dbContext)
        {
            dbContext.DocumentSchemaRegister.RegisterModelSchema<Cat>("1.0.0");
        }
    }
}
