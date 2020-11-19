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

using System;
using System.Threading;

namespace Etherna.MongODM.Core.Utility
{
    public abstract class FreezableConfig : IFreezableConfig, IDisposable
    {
        // Fields.
        private readonly ReaderWriterLockSlim configLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        // Dispose.
        public void Dispose()
        {
            configLock.Dispose();
            GC.SuppressFinalize(this);
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

        protected virtual void FreezeAction() { }
    }
}
