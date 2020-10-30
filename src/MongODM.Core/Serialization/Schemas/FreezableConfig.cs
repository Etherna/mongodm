using System;
using System.Threading;

namespace Etherna.MongODM.Core.Serialization.Schemas
{
    public abstract class FreezableConfig : IFreezableConfig, IDisposable
    {
        // Fields.
        private readonly ReaderWriterLockSlim configLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        // Dispose.
        public void Dispose()
        {
            configLock.Dispose();
        }

        // Properties.
        public bool IsFrozen { get; private set; }

        // Methods.
        public void Freeze()
        {
            configLock.EnterReadLock();
            try
            {
                if (IsFrozen) return;
            }
            finally
            {
                configLock.ExitReadLock();
            }

            configLock.EnterWriteLock();
            try
            {
                if (!IsFrozen)
                {
                    // Execute action.
                    FreezeAction();

                    // Freeze.
                    IsFrozen = true;
                }
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        // Protected methods.
        protected virtual void FreezeAction() { }

        protected void ExecuteConfigAction(Action configAction) =>
            ExecuteConfigAction(() =>
            {
                configAction();
                return 0;
            });

        protected TReturn ExecuteConfigAction<TReturn>(Func<TReturn> configAction)
        {
            if (configAction is null)
                throw new ArgumentNullException(nameof(configAction));

            configLock.EnterWriteLock();
            try
            {
                if (IsFrozen)
                    throw new InvalidOperationException("Configuration is frozen");

                return configAction();
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }
    }
}
