using Etherna.MongODM.Models.Internal.MigrateOpAgg;
using Etherna.MongODM.Serialization;

namespace Etherna.MongODM.Models.Internal.ModelMaps
{
    class MigrateExecutionLogMap : IModelMapsCollector
    {
        public void Register(IDbContext dbContext)
        {
            dbContext.DocumentSchemaRegister.RegisterModelSchema<MigrateExecutionLog>(
                "0.20.0"); //mongodm library's version
        }
    }
}
