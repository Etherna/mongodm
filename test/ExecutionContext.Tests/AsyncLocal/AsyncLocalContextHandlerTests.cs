using Moq;
using Xunit;

namespace Digicando.ExecContext.AsyncLocal
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
