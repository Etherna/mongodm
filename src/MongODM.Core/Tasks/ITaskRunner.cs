using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Tasks
{
    public interface ITaskRunner
    {
        void RunMigrateDbTask(Type dbContextType, string dbMigrationOpId);
        void RunUpdateDocDependenciesTask(Type dbContextType, Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId);
    }
}
