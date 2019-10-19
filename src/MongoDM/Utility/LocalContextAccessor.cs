using System.Threading;

namespace Digicando.MongoDM.Utility
{
    class LocalContextAccessor : ILocalContextAccessor
    {
        // Fields.
        private static readonly AsyncLocal<LocalContext> localContextCurrent = new AsyncLocal<LocalContext>();

        // Properties.
        public LocalContext Context => localContextCurrent.Value;

        // Methods.
        public void OnCreated(LocalContext context) =>
            localContextCurrent.Value = context;

        public void OnDisposed(LocalContext context) =>
            localContextCurrent.Value = null;
    }
}
