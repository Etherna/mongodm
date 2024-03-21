// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.MongODM.AspNetCore.UI.Auth.Handlers;
using Etherna.MongODM.AspNetCore.UI.Auth.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Etherna.MongODM.AspNetCore.UI
{
    public static class ServiceCollectionExtensions
    {
        private const string AreaName = "MongODM";
        private const string FolderPath = "/";
        private const string PolicyName = "mongodmDashboardPolicy";

        public static IServiceCollection AddMongODMAdminDashboard(
            this IServiceCollection services,
            DashboardOptions? dashboardOptions = null)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            dashboardOptions ??= new DashboardOptions();

            services.Configure<RazorPagesOptions>(options =>
            {
                options.Conventions.AuthorizeAreaFolder(AreaName, FolderPath, PolicyName);
                options.Conventions.AddAreaFolderRouteModelConvention(
                    AreaName, FolderPath,
                    routeModel =>
                    {
                        foreach (var selector in routeModel.Selectors)
                            if (selector.AttributeRouteModel?.Template is not null)
                            {
                                var segments = selector.AttributeRouteModel.Template.Split('/');
                                if (segments[0] == AreaName)
                                    segments[0] = dashboardOptions.BasePath;

                                selector.AttributeRouteModel.Template = string.Join("/", segments);
                            }
                    });
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyName, policy =>
                {
                    policy.Requirements.Add(new ValidFiltersRequirement(dashboardOptions.AuthFilters));
                });
            });

            services.AddSingleton<IAuthorizationHandler, ValidFiltersHandler>();

            return services;
        }
    }
}
