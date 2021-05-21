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
using System;
using System.Collections.Generic;
using Xunit;

namespace Etherna.ExecContext
{
    public class ExecutionContextSelectorTests
    {
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
            Mock<IExecutionContext> context0 = new();
            Mock<IExecutionContext> context1 = new();
            context0.SetupGet(c => c.Items)
                .Returns(enableContext1 ? new Dictionary<object, object?> { { "val", "0" } } : null);
            context1.SetupGet(c => c.Items)
                .Returns(enableContext2 ? new Dictionary<object, object?> { { "val", "1" } } : null);
            var selector = new ExecutionContextSelector(new[] { context0.Object, context1.Object });

            // Action.
            var result = selector.Items?["val"] as string;

            // Assert.
            Assert.Equal(expectedResult, result);
        }
    }
}
