using System;
using System.Collections.Generic;

namespace Digicando.MongODM.Tasks
{
    public interface ITaskRunner
    {
        void RunUpdateDocDependenciesTask(Type dbContextType, Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId);
    }
}
