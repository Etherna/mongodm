// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.ExecContext.AsyncLocal;
using Hangfire.Server;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.HF.Filters
{
    public class AsyncLocalContextHangfireFilter : IServerFilter
    {
        // Fields.
        private readonly Dictionary<string, IAsyncLocalContextHandler> contextHandlers = new();
        private readonly IAsyncLocalContext asyncLocalContext;

        // Constructors.
        public AsyncLocalContextHangfireFilter(IAsyncLocalContext asyncLocalContext)
        {
            this.asyncLocalContext = asyncLocalContext;
        }

        // Properties.
        public void OnPerforming(PerformingContext filterContext)
        {
            if (filterContext is null)
                throw new ArgumentNullException(nameof(filterContext));

            lock (contextHandlers)
            {
                contextHandlers.Add(filterContext.BackgroundJob.Id, asyncLocalContext.InitAsyncLocalContext());
            }
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            if (filterContext is null)
                throw new ArgumentNullException(nameof(filterContext));

            lock (contextHandlers)
            {
                var jobId = filterContext.BackgroundJob.Id;
                var context = contextHandlers[jobId];
                contextHandlers.Remove(jobId);
                context.Dispose();
            }
        }
    }
}
