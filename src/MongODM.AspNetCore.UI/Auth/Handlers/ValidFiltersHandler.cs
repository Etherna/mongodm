using Etherna.MongODM.AspNetCore.UI.Auth.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Etherna.MongODM.AspNetCore.UI.Auth.Handlers
{
    class ValidFiltersHandler : AuthorizationHandler<ValidFiltersRequirement>
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
