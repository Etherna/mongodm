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
