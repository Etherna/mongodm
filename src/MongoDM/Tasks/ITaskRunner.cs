using System;
using System.Collections.Generic;

namespace Digicando.MongoDM.Tasks
{
    public interface ITaskRunner
    {
        void RunUpdateDocDependenciesTask(Type modelType, Type keyType, IEnumerable<string> idPaths, object modelId);
    }
}
