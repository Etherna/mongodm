using System;

namespace Digicando.MongoDM.Utility
{
    class LocalContextFactory : ILocalContextFactory
    {
        private readonly ILocalContextAccessor localContextAccessor;

        public LocalContextFactory(
            ILocalContextAccessor localContextAccessor)
        {
            this.localContextAccessor = localContextAccessor;
        }

        public IDisposable CreateNewLocalContext() =>
            new LocalContext(localContextAccessor);
    }
}
