using Etherna.MongODM.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Etherna.MongODM
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDbContextsSeeding(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DbContextsSeedingMiddleware>();
        }
    }
}
