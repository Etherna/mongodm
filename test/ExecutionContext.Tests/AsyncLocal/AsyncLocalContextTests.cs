using System.Threading.Tasks;
using Xunit;

namespace Digicando.ExecContext.AsyncLocal
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
