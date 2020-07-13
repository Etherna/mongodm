using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Tasks
{
    public interface ITaskRunner
    {
        void RunMigrateDbContextTask(Type dbContextType, string migrateOpId);
        void RunUpdateDocDependenciesTask(Type dbContextType, Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId);
    }
}
