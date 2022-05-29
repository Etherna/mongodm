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
