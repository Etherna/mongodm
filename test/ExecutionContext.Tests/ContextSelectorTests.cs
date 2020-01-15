using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Digicando.ExecContext
{
    public class ContextSelectorTests
    {
        [Fact]
        public void NullContextsException()
        {
            Assert.Throws<ArgumentNullException>(() => new ContextSelector(null));
        }

        [Theory]
        [InlineData(false, false, null)]
        [InlineData(false, true, "1")]
        [InlineData(true, false, "0")]
        [InlineData(true, true, "0")]
        public void ContextSelection(
            bool enableContext1,
            bool enableContext2,
            string expectedResult)
        {
            // Setup.
            Mock<IExecutionContext> context0 = new Mock<IExecutionContext>();
            Mock<IExecutionContext> context1 = new Mock<IExecutionContext>();
            context0.SetupGet(c => c.Items)
                .Returns(enableContext1 ? new Dictionary<string, object> { { "val", "0" } } : null);
            context1.SetupGet(c => c.Items)
                .Returns(enableContext2 ? new Dictionary<string, object> { { "val", "1" } } : null);
            var selector = new ContextSelector(new[] { context0.Object, context1.Object });

            // Action.
            var result = selector.Items?["val"] as string;

            // Assert.
            Assert.Equal(expectedResult, result);
        }
    }
}
