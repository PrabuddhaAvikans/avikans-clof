using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Configuration.Application;
using Configuration.Application.Mappings;
using Configuration.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Configuration.Api;

public sealed class ConfigurationModule : IModule
{
    public string Name => ModuleNames.Configuration;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(ConfigurationMappingProfile).Assembly);

        services.AddConfigurationApplication()
            .AddConfigurationInfrastructure();
    }
}
