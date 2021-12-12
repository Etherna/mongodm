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

using Etherna.MongODM.Core.MockHelpers;
using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.Core.Repositories;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Etherna.MongODM.Core
{
    public class ReferenceableInterceptorTest
    {
        private readonly ReferenceableInterceptor<FakeModel, string> interceptor;
        private readonly Mock<ICollectionRepository<FakeModel, string>> repositoryMock;
        private readonly Mock<IDbContext> dbContextMock;

        private readonly Mock<Castle.DynamicProxy.IInvocation> getIsSummaryInvocationMock;
        private readonly Mock<Castle.DynamicProxy.IInvocation> getLoadedMembersInvocationMock;

        public ReferenceableInterceptorTest()
        {
            repositoryMock = new Mock<ICollectionRepository<FakeModel, string>>();

            dbContextMock = new Mock<IDbContext>();
            dbContextMock.Setup(c => c.RepositoryRegistry.ModelRepositoryMap)
                .Returns(() => new Dictionary<Type, IRepository>
                {
                    [typeof(FakeModel)] = repositoryMock.Object
                });
            
            interceptor = new ReferenceableInterceptor<FakeModel, string>(
                new[] { typeof(IReferenceable) },
                dbContextMock.Object);

            getIsSummaryInvocationMock = InterceptorMockHelper.GetExternalPropertyGetInvocationMock<FakeModel, IReferenceable, bool>(
                s => s.IsSummary);
            getLoadedMembersInvocationMock = InterceptorMockHelper.GetExternalPropertyGetInvocationMock<FakeModel, IReferenceable, IEnumerable<string>>(
                s => s.SettedMemberNames);
        }

        [Fact]
        public void InitializeAsSummary()
        {
            // Setup.
            var initializeInvocationMock = InterceptorMockHelper.GetExternalMethodInvocationMock<FakeModel, IReferenceable>(
                nameof(IReferenceable.SetAsSummary),
                new object[]
                {
                    new [] { nameof(FakeModel.Id) }
                });

            // Assert.
            interceptor.Intercept(getIsSummaryInvocationMock.Object);
            interceptor.Intercept(getLoadedMembersInvocationMock.Object);
            Assert.False((bool)getIsSummaryInvocationMock.Object.ReturnValue);
            Assert.Empty(getLoadedMembersInvocationMock.Object.ReturnValue as IEnumerable<string>);

            // Action.
            interceptor.Intercept(initializeInvocationMock.Object);

            // Assert.
            interceptor.Intercept(getIsSummaryInvocationMock.Object);
            interceptor.Intercept(getLoadedMembersInvocationMock.Object);
            Assert.True((bool)getIsSummaryInvocationMock.Object.ReturnValue);
            Assert.Equal(
                new[] { nameof(FakeModel.Id) },
                getLoadedMembersInvocationMock.Object.ReturnValue as IEnumerable<string>);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetLoadedMember(bool isSummary)
        {
            // Setup.
            if (isSummary)
            {
                var initializeInvocationMock = InterceptorMockHelper.GetExternalMethodInvocationMock<FakeModel, IReferenceable>(
                    nameof(IReferenceable.SetAsSummary),
                    new object[]
                    {
                        new[] { nameof(FakeModel.IntegerProp) }
                    });
                interceptor.Intercept(initializeInvocationMock.Object);
            }

            var getPropertyInvocationMock = InterceptorMockHelper.GetPropertyGetInvocationMock<FakeModel, int>(
                m => m.IntegerProp);

            // Action.
            interceptor.Intercept(getPropertyInvocationMock.Object);

            // Assert.
            getPropertyInvocationMock.Verify(i => i.Proceed(), Times.Once());
            repositoryMock.Verify(r => r.TryFindOneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            if (isSummary)
            {
                interceptor.Intercept(getIsSummaryInvocationMock.Object);
                interceptor.Intercept(getLoadedMembersInvocationMock.Object);
                Assert.True((bool)getIsSummaryInvocationMock.Object.ReturnValue);
                Assert.Equal(
                    new[] { nameof(FakeModel.IntegerProp) },
                    getLoadedMembersInvocationMock.Object.ReturnValue as IEnumerable<string>);
            }
            else
            {
                interceptor.Intercept(getIsSummaryInvocationMock.Object);
                interceptor.Intercept(getLoadedMembersInvocationMock.Object);
                Assert.False((bool)getIsSummaryInvocationMock.Object.ReturnValue);
                Assert.Empty(getLoadedMembersInvocationMock.Object.ReturnValue as IEnumerable<string>);
            }
        }

        [Fact]
        public void GetNotLoadedMember()
        {
            // Setup.
            var modelId = "ImAnId";
            var stringValue = "LookAtMe";
            var model = new FakeModel
            {
                Id = modelId,
                IntegerProp = 42
            };

            repositoryMock.Setup(r => r.TryFindOneAsync((object)modelId, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object?>(new FakeModel
                {
                    Id = modelId,
                    IntegerProp = 7,
                    StringProp = stringValue
                }));

            var initializeInvocationMock = InterceptorMockHelper.GetExternalMethodInvocationMock<FakeModel, IReferenceable>(
                nameof(IReferenceable.SetAsSummary),
                new object[] {
                    new[]
                    {
                        nameof(FakeModel.Id),
                        nameof(FakeModel.IntegerProp)
                    }
                });
            interceptor.Intercept(initializeInvocationMock.Object);

            var getPropertyInvocationMock = InterceptorMockHelper.GetPropertyGetInvocationMock(m => m.StringProp, model);

            // Action.
            interceptor.Intercept(getPropertyInvocationMock.Object);

            // Assert.
            getPropertyInvocationMock.Verify(i => i.Proceed(), Times.Once());
            repositoryMock.Verify(r => r.TryFindOneAsync((object)modelId, It.IsAny<CancellationToken>()), Times.Once);

            interceptor.Intercept(getIsSummaryInvocationMock.Object);
            interceptor.Intercept(getLoadedMembersInvocationMock.Object);
            Assert.False((bool)getIsSummaryInvocationMock.Object.ReturnValue);
            
            Assert.Equal(modelId, model.Id);
            Assert.Equal(7, model.IntegerProp);
            Assert.Equal(stringValue, model.StringProp);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetMember(bool isSummary)
        {
            // Setup.
            if (isSummary)
            {
                var initializeInvocationMock = InterceptorMockHelper.GetExternalMethodInvocationMock<FakeModel, IReferenceable>(
                    nameof(IReferenceable.SetAsSummary),
                    new object[] { Array.Empty<string>() });
                interceptor.Intercept(initializeInvocationMock.Object);
            }

            var setPropertyInvocationMock = InterceptorMockHelper.GetPropertySetInvocationMock<FakeModel, int>(
                m => m.IntegerProp, 42);

            // Action.
            interceptor.Intercept(setPropertyInvocationMock.Object);

            // Assert.
            setPropertyInvocationMock.Verify(i => i.Proceed(), Times.Once());
            repositoryMock.Verify(r => r.TryFindOneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            if (isSummary)
            {
                interceptor.Intercept(getIsSummaryInvocationMock.Object);
                interceptor.Intercept(getLoadedMembersInvocationMock.Object);
                Assert.True((bool)getIsSummaryInvocationMock.Object.ReturnValue);
                Assert.Equal(
                    new[] { nameof(FakeModel.IntegerProp) },
                    getLoadedMembersInvocationMock.Object.ReturnValue as IEnumerable<string>);
            }
            else
            {
                interceptor.Intercept(getIsSummaryInvocationMock.Object);
                interceptor.Intercept(getLoadedMembersInvocationMock.Object);
                Assert.False((bool)getIsSummaryInvocationMock.Object.ReturnValue);
            }
        }
    }
}
