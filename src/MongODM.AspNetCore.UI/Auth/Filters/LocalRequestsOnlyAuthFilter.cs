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
