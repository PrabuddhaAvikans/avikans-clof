using ATSolution.Application.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace PeriodClose.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPeriodCloseInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationAssembly, Persistence.PeriodCloseConfigurationAssembly>();
        return services;
    }
}
