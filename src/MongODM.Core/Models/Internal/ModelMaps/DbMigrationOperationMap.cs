using Etherna.MongODM.Models.Internal.DbMigrationOpAgg;
using Etherna.MongODM.Serialization;

namespace Etherna.MongODM.Models.Internal.ModelMaps
{
    class DbMigrationOperationMap : IModelMapsCollector
    {
        public void Register(IDbContext dbContext)
        {
            dbContext.DocumentSchemaRegister.RegisterModelSchema<DbMigrationOperation>(
                "0.20.0");

            dbContext.DocumentSchemaRegister.RegisterModelSchema<MigrationLogBase>(
                "0.20.0");

            dbContext.DocumentSchemaRegister.RegisterModelSchema<DocumentMigrationLog>(
                "0.20.0");

            dbContext.DocumentSchemaRegister.RegisterModelSchema<IndexMigrationLog>(
                "0.20.0");
        }
    }
}
