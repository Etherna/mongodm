using Etherna.MongODM.AspNetCore.UI.Auth.Filters;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;

namespace Etherna.MongODM.AspNetCore.UI.Auth.Requirements
{
    class ValidFiltersRequirement : IAuthorizationRequirement
    {
        public ValidFiltersRequirement(IEnumerable<IDashboardAuthFilter> filters)
        {
            Filters = filters ?? throw new ArgumentNullException(nameof(filters));
        }

        public IEnumerable<IDashboardAuthFilter> Filters { get; }
    }
}
