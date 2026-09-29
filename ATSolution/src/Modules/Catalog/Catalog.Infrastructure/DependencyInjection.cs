using ATSolution.Application.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, Persistence.CatalogConfigurationAssembly>();
        return services;
    }
}
