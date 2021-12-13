using Etherna.MongODM.Core;
using Etherna.MongODM.Core.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etherna.MongODM.AspNetCore.Middlewares
{
    public class DbContextsSeedingMiddleware
    {
        // Fields.
        private readonly RequestDelegate next;
        private readonly IEnumerable<IDbContext> dbContexts;

        // Constructor.
        public DbContextsSeedingMiddleware(
            RequestDelegate next,
            IOptions<MongODMOptions> mongODMOptions,
            IServiceProvider serviceProvider)
        {
            if (mongODMOptions is null)
                throw new ArgumentNullException(nameof(mongODMOptions));

            this.next = next;
            var dbContextTypes = mongODMOptions.Value.DbContextTypes;

            // Get dbcontext instances (cache them).
            dbContexts = dbContextTypes.Select(type => (IDbContext)serviceProvider.GetRequiredService(type))
                                       .ToList();
        }

        // Methods.
        public async Task InvokeAsync(HttpContext context)
        {
            // Seed all dbcontexts.
            foreach (var dbContext in dbContexts)
                if (!dbContext.IsSeeded)
                    await dbContext.SeedIfNeededAsync().ConfigureAwait(false);

            // Call the next delegate/middleware in the pipeline.
            await next(context).ConfigureAwait(false);
        }
    }
}
