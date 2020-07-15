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

using System.Threading.Tasks;
using Xunit;

namespace Etherna.ExecContext.AsyncLocal
{
    public class AsyncLocalContextTests
    {
        private readonly AsyncLocalContext asyncLocalContext;

        public AsyncLocalContextTests()
        {
            asyncLocalContext = new AsyncLocalContext();
        }

        [Fact]
        public void ItemsNullAtCreation()
        {
            // Assert.
            Assert.Null(asyncLocalContext.Items);
        }

        [Fact]
        public async Task AsyncLocalLifeCycle()
        {
            await Task.Run(async () =>
            {
                // Action.
                asyncLocalContext.InitAsyncLocalContext();

                // Assert.
                Assert.NotNull(asyncLocalContext.Items);
                await Task.Run(() =>
                {
                    Assert.NotNull(asyncLocalContext.Items);
                });
            });

            // Assert.
            Assert.Null(asyncLocalContext.Items);
        }

        [Fact]
        public void SyncLocalLifeCycle()
        {
            void localMethod()
            {
                // Action.
                asyncLocalContext.InitAsyncLocalContext();

                // Assert.
                Assert.NotNull(asyncLocalContext.Items);
                void subLocalMethod()
                {
                    Assert.NotNull(asyncLocalContext.Items);
                }
                subLocalMethod();
            }
            localMethod();

            // Assert.
            /* Outside of an async invoker, the container is not automatically disposed. */
            Assert.NotNull(asyncLocalContext.Items);
        }

        [Fact]
        public void ContextDispose()
        {
            // Action.
            using (var handler = asyncLocalContext.InitAsyncLocalContext())
            {
                // Assert.
                Assert.NotNull(asyncLocalContext.Items);
            }

            // Assert.
            Assert.Null(asyncLocalContext.Items);
        }
    }
}
