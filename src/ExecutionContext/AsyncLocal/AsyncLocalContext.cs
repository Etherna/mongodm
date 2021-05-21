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
using System.Collections.Generic;
using System.Threading;

namespace Etherna.ExecContext.AsyncLocal
{
    /// <summary>
    ///     Async local context implementation. This can be used as singleton or with multiple instances.
    ///     The <see cref="AsyncLocal{T}"/> container permits to have an Item instance inside this
    ///     method calling tree.
    /// </summary>
    /// <remarks>
    ///     Before try to use an async local context, call the method <see cref="InitAsyncLocalContext"/>
    ///     for initialize the <see cref="AsyncLocal{T}"/> container, and receive a context handler.
    ///     After have used, dispose the handler for destroy the current context dictionary.
    /// </remarks>
    public class AsyncLocalContext : IAsyncLocalContext, IHandledAsyncLocalContext
    {
        // Fields.
        private static readonly AsyncLocal<IDictionary<object, object?>?> asyncLocalContext = new();

        // Properties.
        public IDictionary<object, object?>? Items => asyncLocalContext.Value;

        // Static properties.
        public static IAsyncLocalContext Instance { get; } = new AsyncLocalContext();

        // Methods.
        public IAsyncLocalContextHandler InitAsyncLocalContext()
        {
            if (asyncLocalContext.Value != null)
                throw new InvalidOperationException("Only one context at time is supported");

            asyncLocalContext.Value = new Dictionary<object, object?>();

            return new AsyncLocalContextHandler(this);
        }

        public void OnDisposed(IAsyncLocalContextHandler context) =>
            asyncLocalContext.Value = null;
    }
}
