using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Etherna.MongODM.AspNetCore.UI.Auth.Filters
{
    public class LocalRequestsOnlyAuthFilter : IDashboardAuthFilter
    {
        public Task<bool> AuthorizeAsync(HttpContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            var localIpAddress = context.Connection.LocalIpAddress.ToString();
            var remoteIpAddress = context.Connection.RemoteIpAddress.ToString();

            // If unknown, assume not local.
            if (string.IsNullOrEmpty(remoteIpAddress))
                return Task.FromResult(false);

            // Check if localhost.
            if (remoteIpAddress == "127.0.0.1" || remoteIpAddress == "::1")
                return Task.FromResult(true);

            // Compare with local address.
            if (remoteIpAddress == localIpAddress)
                return Task.FromResult(true);

            return Task.FromResult(false);
        }
    }
}
