using Etherna.ExecContext.AsyncLocal;
using Etherna.MongODM.HF.Filters;

namespace Hangfire
{
    public static class HangfireGlobalConfigExtension
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public static void UseMongODM(this IGlobalConfiguration config)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // Add a default execution context running with any Hangfire task.
            // Added because with asyncronous task, unrelated to requestes, there is no an alternative context to use with MongODM.
            GlobalJobFilters.Filters.Add(new AsyncLocalContextHangfireFilter(AsyncLocalContext.Instance));
        }
    }
}
