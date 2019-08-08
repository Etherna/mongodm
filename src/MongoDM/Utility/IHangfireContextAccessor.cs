using Hangfire.Server;

namespace Digicando.MongoDM.Utility
{
    internal interface IHangfireContextAccessor
    {
        PerformContext PerformContext { get; }
    }
}