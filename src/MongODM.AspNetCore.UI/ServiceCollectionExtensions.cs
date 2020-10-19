using Etherna.MongODM.AspNetCore.UI;
using Etherna.MongODM.AspNetCore.UI.Auth.Handlers;
using Etherna.MongODM.AspNetCore.UI.Auth.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private const string AreaName = "MongODM";
        private const string FolderPath = "/";
        private const string PolicyName = "mongodmDashboardPolicy";

        public static IServiceCollection AddMongODMAdminDashboard(
            this IServiceCollection services,
            string basePath = "MongODM",
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
                        {
                            var attributeRouteModel = selector.AttributeRouteModel;

                            var segments = selector.AttributeRouteModel.Template.Split('/');
                            if (segments[0] == AreaName)
                                segments[0] = basePath;

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
