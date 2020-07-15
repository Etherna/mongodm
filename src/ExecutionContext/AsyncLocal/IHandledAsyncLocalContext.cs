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

namespace Etherna.ExecContext.AsyncLocal
{
    /// <summary>
    ///     Interface used by <see cref="AsyncLocalContextHandler"/> for comunicate with its
    ///     creator <see cref="AsyncLocalContext"/>.
    /// </summary>
    internal interface IHandledAsyncLocalContext
    {
        void OnDisposed(IAsyncLocalContextHandler context);
    }
}
