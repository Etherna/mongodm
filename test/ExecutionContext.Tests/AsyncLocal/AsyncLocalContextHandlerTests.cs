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

using Moq;
using Xunit;

namespace Etherna.ExecContext.AsyncLocal
{
    public class AsyncLocalContextHandlerTests
    {
        private readonly Mock<IHandledAsyncLocalContext> handledContext;
        private readonly AsyncLocalContextHandler handler;

        public AsyncLocalContextHandlerTests()
        {
            handledContext = new Mock<IHandledAsyncLocalContext>();
            handler = new AsyncLocalContextHandler(handledContext.Object);
        }

        [Fact]
        public void Initialization()
        {
            // Assert.
            Assert.Equal(handledContext.Object, handler.HandledContext);
        }

        [Fact]
        public void HandlerDispose()
        {
            // Action.
            handler.Dispose();

            // Assert.
            handledContext.Verify(c => c.OnDisposed(handler), Times.Once);
        }
    }
}
