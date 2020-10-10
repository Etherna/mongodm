using Etherna.MongODM.Core;
using Etherna.MongODM.Core.Serialization;

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
