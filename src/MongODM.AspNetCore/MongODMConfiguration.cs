using Microsoft.Extensions.DependencyInjection;

namespace Digicando.MongODM.AspNetCore
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
