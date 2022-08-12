//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Etherna.ExecContext.AsyncLocal;
using Etherna.MongODM.Core;
using Etherna.MongODM.Core.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etherna.MongODM
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder SeedDbContexts(
            this IApplicationBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            var serviceProvider = builder.ApplicationServices;
            var mongODMOptions = serviceProvider.GetRequiredService<IOptions<MongODMOptions>>();

            // Get dbcontext instances.
            var dbContextTypes = mongODMOptions.Value.DbContextTypes;
            var dbContexts = dbContextTypes.Select(type => (IDbContext)serviceProvider.GetRequiredService(type));

            // Create an execution context.
            using var execContext = AsyncLocalContext.Instance.InitAsyncLocalContext();

            // Seed all dbcontexts.
            var tasks = new List<Task>();
            foreach (var dbContext in dbContexts)
                if (!dbContext.IsSeeded)
                    tasks.Add(dbContext.SeedIfNeededAsync());

            Task.WaitAll(tasks.ToArray());

            return builder;
        }
    }
}
