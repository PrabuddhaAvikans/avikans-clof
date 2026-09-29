using ATSolution.Application.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Manufacturing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddManufacturingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, Persistence.ManufacturingConfigurationAssembly>();
        return services;
    }
}
