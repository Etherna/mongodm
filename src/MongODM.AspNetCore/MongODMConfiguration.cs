using Microsoft.Extensions.DependencyInjection;

namespace Etherna.MongODM.AspNetCore
{
    public class MongODMConfiguration
    {
        public MongODMConfiguration(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
