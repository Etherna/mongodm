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

using Etherna.MongODM.AspNetCore.UI.Auth.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Etherna.MongODM.AspNetCore.UI.Auth.Handlers
{
    internal sealed class ValidFiltersHandler : AuthorizationHandler<ValidFiltersRequirement>
    {
        // Fields.
        private readonly IHttpContextAccessor httpContextAccessor;

        // Constructor.
        public ValidFiltersHandler(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        // Protected methods.
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ValidFiltersRequirement requirement)
        {
            var httpContext = httpContextAccessor.HttpContext;

            foreach (var filter in requirement.Filters)
            {
                if (!await filter.AuthorizeAsync(httpContext).ConfigureAwait(false))
                    context.Fail();
            }

            context.Succeed(requirement);
        }
    }
}
