// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Etherna.MongODM.AspNetCore.UI.Auth.Filters
{
    public class LocalRequestsOnlyAuthFilter : IDashboardAuthFilter
    {
        public Task<bool> AuthorizeAsync(HttpContext? context)
        {
            if (context is null)
                return Task.FromResult(false);

            var localIpAddress = context.Connection.LocalIpAddress?.ToString();
            var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString();

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
