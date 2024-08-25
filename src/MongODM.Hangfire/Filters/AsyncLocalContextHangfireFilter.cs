﻿// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

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
        public void OnPerforming(PerformingContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            lock (contextHandlers)
            {
                contextHandlers.Add(context.BackgroundJob.Id, asyncLocalContext.InitAsyncLocalContext());
            }
        }

        public void OnPerformed(PerformedContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            lock (contextHandlers)
            {
                var jobId = context.BackgroundJob.Id;
                var contextHandler = contextHandlers[jobId];
                contextHandlers.Remove(jobId);
                contextHandler.Dispose();
            }
        }
    }
}
