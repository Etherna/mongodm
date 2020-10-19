using Etherna.MongODM.AspNetCore.UI.Auth.Filters;
using System.Collections.Generic;

namespace Etherna.MongODM.AspNetCore.UI
{
    public class DashboardOptions
    {
        // Constructor.
        public DashboardOptions()
        {
            AuthFilters = new[] { new LocalRequestsOnlyAuthFilter() };
        }

        // Properties.
        public IEnumerable<IDashboardAuthFilter> AuthFilters { get; set; }
    }
}
