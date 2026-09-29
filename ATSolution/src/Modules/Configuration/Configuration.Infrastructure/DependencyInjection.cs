using ATSolution.Application.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Configuration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, Persistence.ConfigurationConfigurationAssembly>();
        return services;
    }
}
