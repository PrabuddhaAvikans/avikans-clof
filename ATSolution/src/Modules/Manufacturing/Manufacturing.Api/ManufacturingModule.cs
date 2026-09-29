using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Modularity;
using Manufacturing.Application;
using Manufacturing.Application.Mappings;
using Manufacturing.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Manufacturing.Api;

public sealed class ManufacturingModule : IModule
{
    public string Name => ModuleNames.Manufacturing;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(
            _ => { },
            typeof(ManufacturingMappingProfile).Assembly);

        services.AddManufacturingApplication()
            .AddManufacturingInfrastructure();
    }
}
