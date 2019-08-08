using Hangfire.Server;
using System.Threading;

namespace Digicando.MongoDM.Utility
{
    class HangfireContextAccessor : IHangfireContextAccessor, IServerFilter
    {
        // Fields.
        private static readonly AsyncLocal<PerformContext> performContextCurrent = new AsyncLocal<PerformContext>();

        // Properties.
        public PerformContext PerformContext => performContextCurrent.Value;

        public void OnPerformed(PerformedContext filterContext) =>
            performContextCurrent.Value = null;

        public void OnPerforming(PerformingContext filterContext) =>
            performContextCurrent.Value = filterContext;
    }
}
