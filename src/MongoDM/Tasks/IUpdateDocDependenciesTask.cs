using Hangfire;
using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Digicando.MongoDM.Tasks
{
    internal interface IUpdateDocDependenciesTask
    {
        /// <summary>
        /// Needed because https://github.com/sergeyzwezdin/Hangfire.Mongo/issues/165
        /// </summary>
        [Queue(Queues.DB_MAINTENANCE)]
        Task Run_NOTGENERICPROXY_Async(
            PerformContext performContext,
            Type modelType,
            Type keyType,
            IEnumerable<string> idPaths,
            object modelId);
    }
}